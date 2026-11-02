using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal static partial class AssetLifecycleValidationRunner
    {
        private static ISystem NewAudioSystem() =>
            (ISystem)Activator.CreateInstance(Type.GetType("LiteQuark.Runtime.AudioSystem, LiteQuark.Runtime.Addons", true));

        private static object CallAudio(object target, string method, params object[] arguments)
        {
            try { return target.GetType().GetMethod(method).Invoke(target, arguments); }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static T AudioProperty<T>(object audio, string name) => (T)audio.GetType().GetProperty(name).GetValue(audio);

        private sealed class AudioHarness
        {
            internal readonly Harness Assets = New();
            internal readonly ISystem System = NewAudioSystem();
            private readonly IAssetProvider _original;
            internal EmptyGameObjectPool Pool => Get<EmptyGameObjectPool>(System, "_pool");

            internal AudioHarness()
            {
                _original = Get<IAssetProvider>(LiteRuntime.Asset, "_provider");
                Set(LiteRuntime.Asset, "_provider", Assets.Provider);
            }

            internal ulong Play(string path = AudioPath, bool music = false) => (ulong)(music
                ? CallAudio(System, "PlayMusic", path, true, 0.5f, true)
                : CallAudio(System, "PlaySound", path, true, 0, 0.5f, 0f));
            internal object Audio(ulong id) => CallAudio(System, "GetAudio", id);
            internal void Tick() => ((ITick)System).Tick(0.02f);
            internal async UniTask Cleanup()
            {
                try
                {
                    await System.DisposeAsync();
                    await Assets.Shutdown();
                }
                finally { Set(LiteRuntime.Asset, "_provider", _original); }
            }
        }

        private sealed class AudioFailureProbe : IAssetProvider, IDisposable
        {
            private readonly AudioClip _clip = AudioClip.Create("AudioFailureProbe", 4410, 1, 44100, false);
            private Action _deliver;
            internal readonly AudioFailureLease Lease;

            internal sealed class AudioFailureLease : IAssetLease<AudioClip>
            {
                private readonly AudioClip _clip;
                private int _reads;
                internal int Disposals;
                internal AudioFailureLease(AudioClip clip) { _clip = clip; }
                public AudioClip Value => ++_reads == 1 ? _clip : throw new InvalidOperationException("Injected AudioClip result read failure");
                public void Dispose() { Disposals++; }
            }

            internal AudioFailureProbe() { Lease = new AudioFailureLease(_clip); }
            internal void Deliver() { var deliver = _deliver; _deliver = null; deliver(); }
            public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<T>>> callback) where T : UnityEngine.Object
            {
                if (typeof(T) != typeof(AudioClip) || _deliver != null) throw new InvalidOperationException("Unexpected AudioClip probe request.");
                _deliver = () => callback(AssetLoadResult<IAssetLease<T>>.Success((IAssetLease<T>)(object)Lease));
            }
            public void Dispose() { UnityEngine.Object.DestroyImmediate(_clip); }
            public UniTask<bool> Initialize() => UniTask.FromResult(true);
            public UniTask DisposeAsync() => UniTask.CompletedTask;
            public void Tick(float deltaTime) { }
            public string GetVersion() => "audio-failure-probe";
            public bool HasAsset(string assetKey) => assetKey == AudioPath;
            public void UnloadUnusedAssets(int maxDepth) { }
            public void LoadSceneAsync(string sceneKey, LoadSceneParameters parameters, AssetLoadPriority priority,
                Action<AssetLoadResult<IAssetLease<Scene?>>> callback) => throw new NotSupportedException();
        }

        private static Type CreateUIUpdateProbeType(Action<object> onOpen = null, Type bindingAttribute = null)
        {
            // Observe the actual BaseUI callbacks without adding an Addons reference to the core Editor assembly.
            var baseType = Type.GetType("LiteQuark.Runtime.BaseUI, LiteQuark.Runtime.Addons", true);
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UIUpdateValidation"), AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule("UIUpdateValidation").DefineType("UIUpdateProbe", TypeAttributes.Public, baseType);
            type.DefineDefaultConstructor(MethodAttributes.Public);
            if (bindingAttribute != null)
            {
                var node = type.DefineField("BindingNode", typeof(GameObject), FieldAttributes.Public);
                node.SetCustomAttribute(new CustomAttributeBuilder(bindingAttribute.GetConstructor(Type.EmptyTypes), Array.Empty<object>()));
            }
            foreach (var name in new[] { "OnUpdate", "OnClose" })
            {
                var field = type.DefineField(name + "Callback", typeof(Action<object>), FieldAttributes.Public);
                var parameters = name == "OnUpdate" ? new[] { typeof(float) } : Type.EmptyTypes;
                var method = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), parameters);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, typeof(Action<object>).GetMethod("Invoke"));
                il.Emit(OpCodes.Ret);
                type.DefineMethodOverride(method, baseType.GetMethod(name, Private));
            }
            var openCallback = type.DefineField("OnOpenCallback", typeof(Action<object>), FieldAttributes.Public | FieldAttributes.Static);
            var open = type.DefineMethod("OnOpen", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), new[] { typeof(object[]) });
            var openIL = open.GetILGenerator();
            openIL.Emit(OpCodes.Ldsfld, openCallback);
            openIL.Emit(OpCodes.Ldarg_0);
            openIL.Emit(OpCodes.Callvirt, typeof(Action<object>).GetMethod("Invoke"));
            openIL.Emit(OpCodes.Ret);
            type.DefineMethodOverride(open, baseType.GetMethod("OnOpen", Private));
            var probeType = type.CreateType();
            probeType.GetField("OnOpenCallback").SetValue(null, onOpen ?? (_ => { }));
            return probeType;
        }

        private static Type CreateUIBindingProbeAttribute(Action onBind)
        {
            // GetCustomAttribute constructs this derived UINode attribute inside the real UIBinder.
            var baseType = Type.GetType("LiteQuark.Runtime.UINodeAttribute, LiteQuark.Runtime.Addons", true);
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UIBindingValidation"), AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule("UIBindingValidation").DefineType("UIBindingProbeAttribute", TypeAttributes.Public | TypeAttributes.Sealed, baseType);
            var callback = type.DefineField("OnBindCallback", typeof(Action), FieldAttributes.Public | FieldAttributes.Static);
            var constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, string.Empty);
            il.Emit(OpCodes.Call, baseType.GetConstructor(new[] { typeof(string) }));
            il.Emit(OpCodes.Ldsfld, callback);
            il.Emit(OpCodes.Callvirt, typeof(Action).GetMethod("Invoke"));
            il.Emit(OpCodes.Ret);
            var attributeType = type.CreateType();
            attributeType.GetField("OnBindCallback").SetValue(null, onBind);
            return attributeType;
        }

        private sealed class UIOpenHarness
        {
            internal readonly Harness Assets = New();
            internal readonly ISystem System;
            private readonly GameObject _cameraObject = new("UIOpenCamera", typeof(Camera));
            private readonly IAssetProvider _original;
            private readonly Type _probeType;
            private bool _closed;
            internal int Opens;
            internal int Closes;
            internal int Updates;
            internal Action<object> OnOpen = _ => { };
            internal Action<object> OnClose = _ => { };

            internal UIOpenHarness(Type bindingAttribute = null)
            {
                _original = Get<IAssetProvider>(LiteRuntime.Asset, "_provider");
                Set(LiteRuntime.Asset, "_provider", Assets.Provider);
                var systemType = Type.GetType("LiteQuark.Runtime.UISystem, LiteQuark.Runtime.Addons", true);
                System = (ISystem)Activator.CreateInstance(systemType);
                var settingType = Type.GetType("LiteQuark.Runtime.UISystemSetting, LiteQuark.Runtime.Addons", true);
                var setting = Activator.CreateInstance(settingType);
                settingType.GetField("UICamera").SetValue(setting, _cameraObject.GetComponent<Camera>());
                systemType.GetProperty("Setting").SetValue(System, setting);
                _probeType = CreateUIUpdateProbeType(ui =>
                {
                    Opens++;
                    ui.GetType().GetField("OnUpdateCallback").SetValue(ui, (Action<object>)(_ => Updates++));
                    ui.GetType().GetField("OnCloseCallback").SetValue(ui, (Action<object>)(value => { Closes++; OnClose(value); }));
                    OnOpen(ui);
                }, bindingAttribute);
            }

            internal UniTask<object> Open(string path = PrefabPath, bool mutex = false, CancellationToken? ct = null)
            {
                var configType = Type.GetType("LiteQuark.Runtime.UIConfig, LiteQuark.Runtime.Addons", true);
                var depthType = Type.GetType("LiteQuark.Runtime.UIDepthMode, LiteQuark.Runtime.Addons", true);
                var config = Activator.CreateInstance(configType, path, Enum.Parse(depthType, "Normal"), mutex, false);
                var arguments = ct.HasValue
                    ? new object[] { config, ct.Value, Array.Empty<object>() }
                    : new[] { config, Array.Empty<object>() };
                var task = System.GetType().GetMethods().Single(m => m.Name == "OpenUI" && m.GetParameters().Length == arguments.Length)
                    .MakeGenericMethod(_probeType).Invoke(System, arguments);
                return (UniTask<object>)typeof(AssetLifecycleValidationRunner).GetMethod(nameof(AwaitUIOpen), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(_probeType).Invoke(null, new[] { task });
            }

            internal async UniTask<object> OpenReady(string path = PrefabPath)
            {
                var request = Open(path);
                await Wait(() => request.Status.IsCompleted(), "UI open result");
                return await request;
            }

            internal void Tick() => ((ITick)System).Tick(0.02f);
            internal object Find() => System.GetType().GetMethod("FindUI").MakeGenericMethod(_probeType).Invoke(System, null);
            internal void Close(object ui) => System.GetType().GetMethods().Single(m => m.Name == "CloseUI" && !m.IsGenericMethod).Invoke(System, new[] { ui });
            internal GameObject Go(object ui) => (GameObject)_probeType.GetProperty("Go").GetValue(ui);
            internal string State(object ui) => _probeType.GetProperty("State").GetValue(ui).ToString();
            internal UniTask Shutdown()
            {
                _closed = true;
                return System.DisposeAsync();
            }

            internal async UniTask Cleanup()
            {
                try
                {
                    if (!_closed) await Shutdown();
                    await Assets.Shutdown();
                }
                finally
                {
                    Set(LiteRuntime.Asset, "_provider", _original);
                    UnityEngine.Object.DestroyImmediate(_cameraObject);
                    _probeType.GetField("OnOpenCallback").SetValue(null, (Action<object>)(_ => { }));
                }
            }
        }

        private static async UniTask<object> AwaitUIOpen<T>(object task) => await (UniTask<T>)task;

        private static RectTransform GetUICanvasRoot(ISystem system) =>
            (RectTransform)system.GetType().GetProperty("CanvasRoot").GetValue(system);

        private static (IDisposable Entity, CancellationToken Token) NewUIOwnerEntity()
        {
            // Construct the real entity without starting battle logic or adding an assembly reference.
            var type = Type.GetType("LiteBattle.Runtime.LiteUnit, Assembly-CSharp", true);
            var entity = (IDisposable)Activator.CreateInstance(type, new object[] { null });
            return (entity, (CancellationToken)type.GetProperty("LifetimeToken").GetValue(entity));
        }

        private sealed class UIResourceEvent : IEventData { }

        private sealed class UIOpenResourceProbe : IAssetProvider, IDisposable
        {
            private const string SpritePath = "assets/__session_validation/pending_ui_sprite.asset";
            private readonly Texture2D _texture = new(2, 2);
            private readonly Sprite _sprite;
            private readonly SpriteLease _lease;
            private readonly EventSystem _eventSystem = LiteRuntime.Event;
            private object _ui;
            private Type _baseType;
            private AssetHandle<Sprite> _handle;
            private Action _deliver;
            private int _events;
            private int _sprites;

            private sealed class SpriteLease : IAssetLease<Sprite>
            {
                public Sprite Value { get; }
                internal int Disposals;
                private Exception _releaseError;
                internal SpriteLease(Sprite sprite, Exception releaseError) { Value = sprite; _releaseError = releaseError; }
                public void Dispose()
                {
                    Disposals++;
                    var error = _releaseError;
                    _releaseError = null;
                    if (error != null) throw error;
                }
            }

            internal UIOpenResourceProbe(Exception releaseError = null)
            {
                _sprite = Sprite.Create(_texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
                _lease = new SpriteLease(_sprite, releaseError);
            }

            internal void Acquire(object ui)
            {
                _ui = ui;
                _baseType = ui.GetType().BaseType;
                _baseType.GetMethod("RegisterEvent", Private).MakeGenericMethod(typeof(UIResourceEvent))
                    .Invoke(ui, new object[] { (Action<UIResourceEvent>)(_ => _events++) });
                _eventSystem.Send(new UIResourceEvent());
                Check(_events == 1, "The UI callback registers a working listener under the actual UI instance's event tag.");
                _events = 0;

                var original = Get<IAssetProvider>(LiteRuntime.Asset, "_provider");
                try
                {
                    Set(LiteRuntime.Asset, "_provider", this);
                    _baseType.GetMethod("LoadSprite").Invoke(ui, new object[] { SpritePath, (Action<Sprite>)(_ => _sprites++) });
                }
                finally { Set(LiteRuntime.Asset, "_provider", original); }
                _handle = ((List<AssetHandle<Sprite>>)_baseType.GetField("_spriteHandleList", Private).GetValue(ui)).Single();
                Check(_handle.State == AssetHandleState.Pending && _deliver != null,
                    "The real BaseUI.LoadSprite path owns an unfinished Handle before the UI callback exits.");
            }

            internal void DeliverSprite()
            {
                _deliver();
                _deliver = null;
            }

            internal void VerifyReleased(bool completed = false)
            {
                var released = _handle.IsDisposed && _handle.State == (completed ? AssetHandleState.Succeeded : AssetHandleState.Cancelled);
                _eventSystem.Send(new UIResourceEvent());
                if (!completed) DeliverSprite();
                Check(released, "The UI disposes its Sprite Handle before final resource verification.");
                Check(_events == 0 && _sprites == (completed ? 1 : 0), "Released UI resources receive neither events nor late Sprite callbacks.");
                Check(_sprite != null && _lease.Disposals == 1 && _handle.Result == null,
                    "The valid Sprite lease is returned exactly once instead of remaining owned by the UI.");
                Check(((List<AssetHandle<Sprite>>)_baseType.GetField("_spriteHandleList", Private).GetValue(_ui)).Count == 0 &&
                    (!_eventSystem.GetEventDebugInfo().TryGetValue(typeof(UIResourceEvent), out var count) || count == 0),
                    "Cleanup empties both the UI's Sprite Handle list and its tagged listener registration.");
                var cleanup = _baseType.GetMethod("ReleaseResources", Private);
                cleanup.Invoke(_ui, null);
                cleanup.Invoke(_ui, null);
                Check(_lease.Disposals == 1 && _eventSystem.GetEventDebugInfo()[typeof(UIResourceEvent)] == 0,
                    "Repeated framework cleanup after the GameObject reference was cleared does not release ownership twice.");
            }

            public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<T>>> callback) where T : UnityEngine.Object
            {
                if (typeof(T) != typeof(Sprite) || assetKey != SpritePath || _deliver != null) throw new InvalidOperationException("Unexpected UI Sprite request.");
                _deliver = () => callback(AssetLoadResult<IAssetLease<T>>.Success((IAssetLease<T>)(object)_lease));
            }
            public UniTask<bool> Initialize() => UniTask.FromResult(true);
            public UniTask DisposeAsync() => UniTask.CompletedTask;
            public void Tick(float deltaTime) { }
            public string GetVersion() => "ui-resource-probe";
            public bool HasAsset(string assetKey) => assetKey == SpritePath;
            public void UnloadUnusedAssets(int maxDepth) { }
            public void LoadSceneAsync(string sceneKey, LoadSceneParameters parameters, AssetLoadPriority priority,
                Action<AssetLoadResult<IAssetLease<Scene?>>> callback) => throw new NotSupportedException();

            public void Dispose()
            {
                _handle?.Dispose();
                if (_ui != null) _eventSystem.UnRegisterAll((int)_baseType.GetField("_eventTag", Private).GetValue(_ui));
                _deliver?.Invoke();
                _deliver = null;
                UnityEngine.Object.DestroyImmediate(_sprite);
                UnityEngine.Object.DestroyImmediate(_texture);
            }
        }

        private sealed class DeferredTemplatePool : BaseGameObjectPool
        {
            public DeferredTemplatePool() { }
            internal void Finish(GameObject template) => OnLoadTemplate(template);
        }

        private sealed class ThrowingReleasePool : EmptyGameObjectPool
        {
            internal readonly Exception Error = new InvalidOperationException("Injected pool release failure");
            private int _releases;
            public ThrowingReleasePool() { }

            protected override void OnRelease(GameObject go)
            {
                if (++_releases == 2) throw Error;
                base.OnRelease(go);
            }
        }

        private static void BuildFixtures()
        {
            if (AssetDatabase.IsValidFolder(Fixture))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(Fixture);
            }
            Directory.CreateDirectory(Fixture);
            Directory.CreateDirectory(BundleFolder);
            File.WriteAllText(Fixture + "/payload.txt", "LiteQuark session validation");
            File.WriteAllText(Fixture + "/second.txt", "Second asset in the shared text bundle");
            // A real imported PCM clip exercises Bundle loading without relying on an audio output device.
            using (var writer = new BinaryWriter(File.Create(Fixture + "/tone.wav")))
            {
                const int samples = 4410;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(44100);
                writer.Write(88200);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples * 2);
                for (var i = 0; i < samples; i++) writer.Write((short)(Math.Sin(i * 2 * Math.PI * 440 / 44100) * 1000));
            }
            Directory.CreateDirectory("Assets/StandaloneAssets/_session_validation");
            File.WriteAllText(EditorTextPath, "Editor Asset Lease validation");
            AssetDatabase.Refresh();
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) throw new Exception("No fixture shader available.");
            var material = new Material(shader) { color = Color.cyan };
            AssetDatabase.CreateAsset(material, Fixture + "/shared.mat");
            foreach (var name in new[] { "a", "c" })
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "validation_" + name;
                go.GetComponent<Renderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(go, Fixture + "/" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "SessionValidationSceneMarker";
            marker.GetComponent<Renderer>().sharedMaterial = material;
            EditorSceneManager.SaveScene(scene, Fixture + "/validation_scene.unity");
            EditorSceneManager.SaveScene(scene, EditorScenePath, true);
            EditorSceneManager.SaveScene(scene, EditorSecondScenePath, true);
            var host = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(host, Fixture + "/host.unity");
            AssetDatabase.SaveAssets();
            var builds = new[]
            {
                Build("text.ab", "payload.txt", "second.txt"), Build("b.ab", "shared.mat"),
                Build("a.ab", "a.prefab"), Build("c.ab", "c.prefab"), Build("scene.ab", "validation_scene.unity"),
                Build("audio.ab", "tone.wav")
            };
            var manifest = BuildPipeline.BuildAssetBundles(BundleFolder, builds,
                BuildAssetBundleOptions.UncompressedAssetBundle, ValidationEnvironment.BundleTarget);
            if (manifest == null) throw new Exception("Fixture bundle build failed.");
            if (!manifest.GetAllDependencies("a.ab").Contains("b.ab") ||
                !manifest.GetAllDependencies("c.ab").Contains("b.ab") ||
                !manifest.GetAllDependencies("scene.ab").Contains("b.ab"))
                throw new Exception("Fixtures did not produce the required real shared dependency.");
            File.WriteAllText(Path.Combine(ValidationEnvironment.Reports("lifecycle"), "fixture-dependencies.txt"),
                string.Join("\n", manifest.GetAllAssetBundles().Select(b => b + " -> " + string.Join(",", manifest.GetAllDependencies(b)))));
        }

        private static AssetBundleBuild Build(string bundle, params string[] assets) =>
            new() { assetBundleName = bundle, assetNames = assets.Select(asset => Fixture + "/" + asset).ToArray() };

        private sealed class Harness
        {
            internal readonly AssetBundleProvider Provider = new();
            internal readonly AssetSystem Assets = new();
            internal readonly Locator Locater;
            internal readonly List<IDisposable> Handles = new();
            internal bool Disposed;
            internal bool ShutdownFailed;
            internal HashSet<string> LoadingNames => Get<HashSet<string>>(Provider, "_loadingSceneSet");
            internal List<SceneInfoCache> SceneEntries() => Get<List<SceneInfoCache>>(Cache("scene.ab"), "_sceneCacheMap");
            internal SceneHandle Scene(CancellationToken token = default)
            {
                var handle = Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive), ct: token);
                Handles.Add(handle);
                return handle;
            }
            internal async UniTask Shutdown()
            {
                if (Disposed) return;
                try
                {
                    var shutdown = Assets.DisposeAsync();
                    await Wait(() => shutdown.Status.IsCompleted(), "Asset fixture shutdown");
                    await shutdown;
                    Disposed = true;
                }
                catch
                {
                    ShutdownFailed = true;
                    throw;
                }
            }
            internal Harness(bool manual, int concurrency = 10, bool scheduleManual = false)
            {
                Locater = new Locator(manual, scheduleManual);
                var pack = new VersionPackInfo("validation", "windows", false, false, new[]
                {
                    new BundleInfo(1, "text.ab", new[] { TextPath, SecondTextPath, "assets/__session_validation/missing.txt" }, null),
                    new BundleInfo(2, "b.ab", new[] { MaterialPath }, null),
                    new BundleInfo(3, "a.ab", new[] { PrefabPath }, new[] { "b.ab" }),
                    new BundleInfo(4, "c.ab", new[] { "assets/__session_validation/c.prefab" }, new[] { "b.ab" }),
                    new BundleInfo(5, "scene.ab", new[] { ScenePath, "assets/__session_validation/nonexistent_scene.unity" }, new[] { "b.ab" }),
                    new BundleInfo(6, "audio.ab", new[] { AudioPath }, null)
                });
                pack.Initialize();
                Set(Provider, "_packInfo", pack);
                Set(Provider, "_bundleLoader", new AssetBundleLoader(Locater, pack, concurrency));
                Set(Assets, "_provider", Provider);
            }
            internal AssetHandle<T> Asset<T>(string path, CancellationToken token = default) where T : UnityEngine.Object
            {
                var handle = Assets.LoadAssetHandle<T>(path, ct: token); Handles.Add(handle); return handle;
            }
            internal bool Has(string path) => Provider.TryGetBundleCache(path, out _);
            internal AssetBundleCache Cache(string path) => Provider.GetOrCreateBundleCache(path);
            internal int Refs(string path) => Get<int>(Cache(path), "_refCount");
            internal AssetInfoCache Entry(string bundle, string path, Type type = null) => Get<Dictionary<(string, Type), AssetInfoCache>>(Cache(bundle), "_assetCacheMap")[(path, type ?? typeof(TextAsset))];
            internal bool HasAssetEntry(string bundle, string path, Type type = null) => Has(bundle) && Get<Dictionary<(string, Type), AssetInfoCache>>(Cache(bundle), "_assetCacheMap").ContainsKey((path, type ?? typeof(TextAsset)));
            internal int AssetRefs(string bundle, string path, Type type = null) => Get<int>(Entry(bundle, path, type), "_refCount");
            internal AssetBundle Finish(string path, bool success)
            {
                AssetBundle bundle = null;
                if (success)
                {
                    bundle = AssetBundle.LoadFromFile(Path.Combine(BundleFolder, path));
                    if (bundle == null) throw new Exception("Controlled bundle could not load: " + path);
                    Locater.Opened.Add(bundle);
                }
                Locater.Manual[path].Finish(bundle);
                Get<AssetBundleLoader>(Provider, "_bundleLoader").Tick(0);
                return bundle;
            }
        }

        private sealed class FileRemoteLocator : IBundleLocater
        {
            internal LoadRemoteBundleTask Task;
            internal string UriOverride;
            public LoadVersionPackTask LoadVersionPack(string name, Action<VersionPackInfo> callback) => throw new NotSupportedException();
            public ILoadBundleTask LoadBundle(string path, string hash, Action<AssetBundle> callback)
            {
                Task = LiteRuntime.Task.AddLoadRemoteBundleTask(UriOverride ?? new Uri(Path.Combine(BundleFolder, path)).AbsoluteUri, null, callback);
                return Task;
            }
        }

        private sealed class ThrowingDownloadHandler : DownloadHandlerScript
        {
            internal readonly Exception Error;
            internal Action BeforeThrow;
            internal int Throws;
            internal ThrowingDownloadHandler(Exception error) { Error = error; }
            public override void Dispose()
            {
                base.Dispose();
                if (Throws != 0) return;
                Throws++;
                BeforeThrow?.Invoke();
                throw Error;
            }
        }

        private sealed class CleanupProbe : BaseTask, IResourceTaskCleanup
        {
            private readonly UniTaskCompletionSource _cleanup = new();
            public UniTask WaitForCleanupAsync() => _cleanup.Task;
            internal void FinishCleanup(Exception error = null)
            {
                if (error == null) _cleanup.TrySetResult();
                else _cleanup.TrySetException(error);
            }
            protected override void OnExecute() { }
        }

        private sealed class RequestReleaseProbe : UnityDownloadBaseTask, IResourceTaskCleanup
        {
            private readonly Exception _releaseError;
            private readonly UniTaskCompletionSource _cleanup = new();
            internal readonly List<UnityWebRequest> Requests = new();
            internal ThrowingDownloadHandler Handler;
            internal ITimer RetryTimerAtRelease;
            internal int Failures;
            internal int CleanupReports;
            internal RequestReleaseProbe(string uri, Exception error, bool retry)
                : base(uri, new RetryParam(retry ? 1 : 60, retry ? 1 : 0, 30f), false) { _releaseError = error; }
            public UniTask WaitForCleanupAsync() => _cleanup.Task;
            internal bool CompleteSuccessfully() => Complete(true);
            protected override UnityWebRequest CreateRequest()
            {
                Handler = new ThrowingDownloadHandler(_releaseError);
                Handler.BeforeThrow = () =>
                {
                    var id = (ulong)typeof(UnityDownloadBaseTask).GetField("_retryTimerID", Private).GetValue(this);
                    RetryTimerAtRelease = LiteRuntime.Timer.FindTimer(id);
                };
                var request = new UnityWebRequest(Uri, UnityWebRequest.kHttpVerbGET, Handler, null);
                Requests.Add(request);
                return request;
            }
            protected override void OnSuccess(UnityWebRequest request) { Complete(true); }
            protected override void OnFailed() { Failures++; }
            protected override void OnResourcesReleased(Exception error)
            {
                CleanupReports++;
                if (error == null) _cleanup.TrySetResult();
                else _cleanup.TrySetException(error);
            }
        }

        private sealed class BundleHttpServer : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly System.Threading.Tasks.TaskCompletionSource<bool> _bodyGate = new(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
            private TcpClient _client;
            private volatile bool _disposed;
            internal readonly string Url;
            internal readonly System.Threading.Tasks.Task Work;

            internal BundleHttpServer(byte[] body)
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/bundle.ab";
                Work = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                        _client = client;
                        if (_disposed) return;
                        using var stream = client.GetStream();
                        var header = new List<byte>();
                        var buffer = new byte[1];
                        while (header.Count < 8192)
                        {
                            if (await stream.ReadAsync(buffer, 0, 1).ConfigureAwait(false) == 0) throw new IOException("Missing HTTP request header.");
                            header.Add(buffer[0]);
                            var n = header.Count;
                            if (n >= 4 && header[n - 4] == 13 && header[n - 3] == 10 && header[n - 2] == 13 && header[n - 1] == 10) break;
                        }
                        var response = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                        var split = Math.Max(1, body.Length / 2);
                        await stream.WriteAsync(body, 0, split).ConfigureAwait(false);
                        await stream.FlushAsync().ConfigureAwait(false);
                        await _bodyGate.Task.ConfigureAwait(false);
                        if (_disposed) return;
                        await stream.WriteAsync(body, split, body.Length - split).ConfigureAwait(false);
                        await stream.FlushAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (_disposed)
                    {
                        // Closing the fixture interrupts its pending socket operation.
                    }
                });
            }

            internal void ReleaseBody() => _bodyGate.TrySetResult(true);

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _bodyGate.TrySetResult(true);
                _client?.Dispose();
                _listener.Stop();
            }
        }

        private sealed class Locator : IBundleLocater
        {
            private readonly bool _manual;
            private readonly bool _scheduleManual;
            internal bool ThrowOnCreate;
            internal readonly Dictionary<string, int> Calls = new();
            internal readonly List<string> Started = new();
            internal readonly Dictionary<string, ManualTask> Manual = new();
            internal readonly List<AssetBundle> Opened = new();
            internal Locator(bool manual, bool scheduleManual = false) { _manual = manual; _scheduleManual = scheduleManual; }
            public LoadVersionPackTask LoadVersionPack(string name, Action<VersionPackInfo> callback) => throw new NotSupportedException();
            public ILoadBundleTask LoadBundle(string path, string hash, Action<AssetBundle> callback)
            {
                Calls[path] = Calls.TryGetValue(path, out var count) ? count + 1 : 1;
                Started.Add(path);
                if (ThrowOnCreate) throw new InvalidOperationException("Injected locator failure");
                if (_manual)
                {
                    var task = new ManualTask(callback);
                    Manual[path] = task;
                    if (_scheduleManual) LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
                    else task.Execute();
                    return task;
                }
                return LiteRuntime.Task.AddLoadLocalBundleTask(Path.Combine(BundleFolder, path), callback);
            }
            internal void FinishOutstanding()
            {
                foreach (var task in Manual.Values.ToArray()) if (!task.IsDone) task.Finish(null);
            }
        }

        private sealed class ManualTask : BaseTask, ILoadBundleTask
        {
            private Action<AssetBundle> _callback;
            internal ManualTask(Action<AssetBundle> callback = null) { _callback = callback; }
            public AssetBundle GetBundle() => Result as AssetBundle;
            internal void Finish(AssetBundle bundle)
            {
                var callback = _callback;
                _callback = null;
                if (Complete(bundle)) LiteUtils.SafeInvoke(callback, bundle);
            }
            protected override void OnTerminated()
            {
                if (State != TaskState.Failed) return;
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback, null);
            }
            protected override void OnExecute() { }
            protected override void OnDispose() { _callback = null; }
        }
    }
}
