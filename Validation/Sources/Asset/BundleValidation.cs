using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading;

namespace LiteQuark.Editor
{
    internal abstract partial class BundleValidation
    {
        protected const string PendingKey = "LiteQuark.SessionValidation.Pending";
        protected const string Fixture = "Assets/__session_validation";
        protected const string TextPath = "assets/__session_validation/payload.txt";
        protected const string SecondTextPath = "assets/__session_validation/second.txt";
        protected const string PrefabPath = "assets/__session_validation/a.prefab";
        protected const string MaterialPath = "assets/__session_validation/shared.mat";
        protected const string AudioPath = "assets/__session_validation/tone.wav";
        protected const string EffectPath = "assets/__session_validation/effect.prefab";
        protected const string ScenePath = "assets/__session_validation/validation_scene.unity";
        protected const string EditorSceneFolder = "Assets/StandaloneAssets/_session_validation";
        protected const string EditorScenePath = EditorSceneFolder + "/editor_validation_scene.unity";
        protected const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        protected static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
        protected static string BundleFolder => Path.Combine(Root, "Bundles", SessionState.GetString(PendingKey + ".Suite", "tasks"));
        protected static readonly ValidationReport Results = new();
        private static readonly List<string> Errors = new();
        protected static Harness Current;
        private static bool _running;
        protected static ValidationCase _case;

        protected static void BeginSuite(string suite, Func<AssetBundleBuild[]> media = null)
        {
            try
            {
                SessionState.SetString(PendingKey + ".Suite", suite);
                BuildFixtures(suite, media);
                SessionState.SetBool(PendingKey, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
                Results.Save(SessionState.GetString(PendingKey + ".Suite", string.Empty));
                Debug.LogException(ex);
                EditorApplication.Exit(2);
            }
        }

        protected static void Resume(string suite, Func<UniTask> scenarios)
        {
            void Start()
            {
                if (_running || !EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false) ||
                    SessionState.GetString(PendingKey + ".Suite", string.Empty) != suite) return;
                _running = true;
                SessionState.SetBool(PendingKey, false);
                RunAll(scenarios).Forget();
            }

            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredPlayMode) Start(); };
            EditorApplication.delayCall += Start;
        }

        private static async UniTask RunAll(Func<UniTask> scenarios)
        {
            Application.logMessageReceived += CaptureLog;
            try
            {
                new GameObject("SessionValidationLauncher").AddComponent<LiteLauncher>();
                var deadline = Time.realtimeSinceStartup + 30;
                while (LiteRuntime.Instance.State != LiteRuntime.RuntimeState.Running)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new Exception("Runtime startup timed out.");
                    await UniTask.Yield();
                }
                await scenarios();
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
                Results.Save(SessionState.GetString(PendingKey + ".Suite", string.Empty));
                EditorApplication.Exit(Results.fatalError == null && Results.cases.All(c => c.passed) ? 0 : 2);
            }
        }

        protected static async UniTask Run(string id, Func<UniTask> action, params string[] expected)
        {
            _case = new ValidationCase { id = id };
            Results.cases.Add(_case);
            Errors.Clear();
            var start = Time.realtimeSinceStartup;
            LiteRuntime.Setting.Asset.EnableRetain = true;
            LiteRuntime.Setting.Asset.BundleRetainTime = 300;
            try
            {
                await action();
                _case.passed = true;
            }
            catch (Exception ex)
            {
                _case.failure = ex.ToString();
            }
            finally
            {
                try { await Cleanup(); }
                catch (Exception ex) { _case.passed = false; _case.failure += "\nCleanup: " + ex; }
                _case.errors.AddRange(Errors);
                var unexpected = Errors.Where(e => !expected.Any(e.Contains)).ToArray();
                if (unexpected.Length > 0)
                {
                    _case.passed = false;
                    _case.failure += "\nUnexpected errors: " + string.Join("\n", unexpected);
                }
                _case.seconds = Time.realtimeSinceStartup - start;
                Debug.Log("SESSION_VALIDATION " + (_case.passed ? "PASS " : "FAIL ") + id);
                Results.Save(SessionState.GetString(PendingKey + ".Suite", string.Empty));
                _case = null;
            }
        }

        private static void CaptureLog(string message, string stack, LogType type)
        {
            if (_case != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Errors.Add(message);
        }

        protected static async UniTask Wait(Func<bool> predicate, string operation, float seconds = 12)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > end) throw new TimeoutException(operation);
                await Frame();
            }
        }

        protected static async UniTask Frame()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (Current != null && !Current.Disposed) Current.Provider.Tick(0);
        }

        protected static async UniTask Cleanup()
        {
            if (Current == null) return;
            foreach (var handle in Current.Handles) handle.Dispose();
            Current.Locater.FinishOutstanding();
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(s => s.name == "validation_scene" || s.name.StartsWith("editor_validation_", StringComparison.Ordinal)).ToArray();
            foreach (var scene in scenes)
            {
                if (!string.IsNullOrEmpty(_case.failure) && scene.IsValid() && scene.isLoaded)
                {
                    // Test isolation only: do not let a failed production ownership path contaminate later cases.
                    _case.evidence.Add("Harness-only cleanup of leaked fixture Scene " + scene.name + "#" + scene.handle +
                        "; this is not counted as successful production cleanup.");
                    SceneManager.UnloadSceneAsync(scene);
                }
                try
                {
                    await Wait(() => !scene.IsValid() || !scene.isLoaded, "Scene cleanup", 15);
                }
                catch (TimeoutException ex)
                {
                    _case.passed = false;
                    _case.failure += "\nProduction scene cleanup did not finish: " + scene.name + "#" + scene.handle + "\n" + ex;
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        _case.evidence.Add("Harness-only cleanup after timeout for Scene " + scene.name + "#" + scene.handle +
                            "; the production cleanup failure remains a failed case.");
                        SceneManager.UnloadSceneAsync(scene);
                        await Wait(() => !scene.IsValid() || !scene.isLoaded, "Harness scene isolation cleanup", 15);
                    }
                }
            }
            if (!Current.Disposed)
            {
                Current.Assets.UnloadUnusedAssets(20);
                await Current.Shutdown();
            }
            foreach (var bundle in Current.Locater.Opened)
            {
                if (bundle == null) continue;
                _case.passed = false;
                _case.evidence.Add("Harness-only cleanup of native Bundle " + bundle.name);
                _case.failure += "\nFixture shutdown left a native Bundle loaded.";
                bundle.Unload(true);
            }
            Current = null;
            await UniTask.Yield();
        }

        protected static Harness New(bool manual = false, int concurrency = 10)
        {
            Current = new Harness(manual, concurrency);
            return Current;
        }

        protected static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);

        protected static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);

        protected static void Check(bool condition, string message) => _case.Check(condition, message);

        protected static async UniTask WaitWithoutLoaderTick(Func<bool> predicate, string operation)
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(operation);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }
        }

        protected static List<ITask> TaskSnapshot()
        {
            var tasks = new List<ITask>();
            LiteRuntime.Task.CopyTasksTo(tasks);
            return tasks;
        }

        protected static List<GateTask> FillGroup(int groupId, int count)
        {
            var tasks = new List<GateTask>();
            for (var i = 0; i < count; i++)
            {
                var task = new GateTask();
                tasks.Add(task);
                LiteRuntime.Task.AddTask(task, groupId);
            }
            return tasks;
        }

        protected static async UniTask ReleaseGates(List<GateTask> gates)
        {
            foreach (var task in gates) task.Cancel();
            await Wait(() => gates.All(task => task.Disposed), "Gate tasks retired");
        }

        protected sealed class GateTask : BaseTask
        {
            internal bool Disposed;
            protected override void OnExecute() { }
            protected override void OnDispose() { Disposed = true; }
        }

        protected static AssetBundle NativeBundle(IBundleResource resource) => resource == null ? null : Get<AssetBundle>(resource, "_bundle");

        protected sealed class Harness
        {
            internal readonly AssetManager Provider = new();
            internal readonly AssetSystem Assets = new();
            internal readonly Locator Locater;
            internal readonly List<IDisposable> Handles = new();
            internal bool Disposed;
            internal SceneHandle Scene(CancellationToken token = default)
            {
                var handle = Assets.LoadSceneHandle(ScenePath, ct: token);
                Handles.Add(handle);
                return handle;
            }
            internal async UniTask Shutdown()
            {
                if (Disposed) return;
                var shutdown = Assets.DisposeAsync();
                await Wait(() => shutdown.Status.IsCompleted(), "Asset fixture shutdown");
                await shutdown;
                Disposed = true;
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
                    new BundleInfo(6, "audio.ab", new[] { AudioPath }, null),
                    new BundleInfo(7, "effect.ab", new[] { EffectPath }, null)
                });
                pack.Initialize();
                Set(Provider, "_packInfo", pack);
                Set(Provider, "_bundleLoader", new AssetBundleLoader(Locater, pack, concurrency));
                Set(Assets, "_manager", Provider);
            }
            internal AssetHandle<T> Asset<T>(string path, CancellationToken token = default) where T : UnityEngine.Object
            {
                var handle = Assets.LoadAssetHandle<T>(path, ct: token); Handles.Add(handle); return handle;
            }
            internal bool Has(string path) => Provider.TryGetBundleCache(path, out _);
            internal AssetBundleCache Cache(string path) => Provider.GetOrCreateBundleCache(path);
            internal int Refs(string path) => Get<int>(Cache(path), "_refCount");
            internal AssetInfoCache Entry(string bundle, string path, Type type = null) => Get<Dictionary<(string, Type), AssetInfoCache>>(Cache(bundle), "_assetCacheMap")[(path, type ?? typeof(TextAsset))];
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

        protected sealed class Locator : IBundleProvider
        {
            private readonly bool _manual;
            private readonly bool _scheduleManual;
            internal readonly Dictionary<string, int> Calls = new();
            internal readonly List<string> Started = new();
            internal readonly Dictionary<string, ManualTask> Manual = new();
            internal readonly List<AssetBundle> Opened = new();
            internal Locator(bool manual, bool scheduleManual = false) { _manual = manual; _scheduleManual = scheduleManual; }
            public BaseTask LoadVersionPack(string name) => throw new NotSupportedException();
            public ILoadBundleTask LoadBundle(string path, string hash)
            {
                Calls[path] = Calls.TryGetValue(path, out var count) ? count + 1 : 1;
                Started.Add(path);
                if (_manual)
                {
                    var task = new ManualTask();
                    Manual[path] = task;
                    if (_scheduleManual) LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
                    else task.Execute();
                    return task;
                }
                var localTask = new LoadLocalBundleTask(Path.Combine(BundleFolder, path));
                LiteRuntime.Task.AddTask(localTask, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
                return localTask;
            }
            internal void FinishOutstanding()
            {
                foreach (var task in Manual.Values.ToArray()) if (!task.IsDone) task.Finish(null);
            }
        }

        protected sealed class ManualTask : BaseTask, ILoadBundleTask
        {
            public IBundleResource GetBundle() => Result as IBundleResource;
            internal void Finish(AssetBundle bundle)
            {
                if (IsDone)
                {
                    if (bundle != null && (State != TaskState.Succeeded || !ReferenceEquals(NativeBundle(GetBundle()), bundle)))
                        bundle.Unload(true);
                    return;
                }
                Complete(bundle != null ? new AssetBundleResource(bundle) : null);
            }
            protected override void OnExecute() { }
        }
    }
}
