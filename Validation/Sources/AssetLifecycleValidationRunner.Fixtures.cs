using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
                Build("a.ab", "a.prefab"), Build("c.ab", "c.prefab"), Build("scene.ab", "validation_scene.unity")
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
                    new BundleInfo(5, "scene.ab", new[] { ScenePath, "assets/__session_validation/nonexistent_scene.unity" }, new[] { "b.ab" })
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
