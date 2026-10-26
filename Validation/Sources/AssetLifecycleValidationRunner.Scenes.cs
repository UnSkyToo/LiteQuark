using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal static partial class AssetLifecycleValidationRunner
    {
        private static async UniTask ProviderShutdownSceneGuard()
        {
            var h = New(true);
            var first = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0);
            var native = h.Finish("text.ab", true);
            SceneHandle retry = null;
            async UniTask RetryScene()
            {
                await first;
                retry = h.Scene();
                Check(retry.IsDone && retry.Result == null && h.LoadingNames.Count == 0 && !h.Has("scene.ab"),
                    "Scene retry during Provider disposal completes as failure before name registration or cache creation.");
                h.Provider.Dispose();
            }
            var continuation = RetryScene();
            h.Shutdown();
            await continuation;
            var assetCallbacks = 0;
            var sceneCallbacks = 0;
            var unexpectedLease = false;
            h.Provider.LoadAssetAsync<TextAsset>(TextPath, AssetLoadPriority.Normal, result =>
            {
                assetCallbacks++;
                unexpectedLease |= result.Value != null;
                result.Value?.Dispose();
                Check(!result.Succeeded && result.Value == null && result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message),
                    "Closed Provider delivers a failure outcome with no owned Lease.");
            });
            h.Provider.LoadSceneAsync(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive), AssetLoadPriority.Normal, result =>
            {
                sceneCallbacks++;
                unexpectedLease |= result.Value != null;
                result.Value?.Dispose();
                Check(!result.Succeeded && result.Value == null && result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message),
                    "Closed Provider delivers a failure outcome with no owned Lease.");
            });
            h.Provider.Dispose();
            Check(assetCallbacks == 1 && sceneCallbacks == 1 && !unexpectedLease,
                "Closed Provider returns failure before touching cleared pack metadata.");
            Check(native == null && Get<Dictionary<string, AssetBundleCache>>(h.Provider, "_bundleCacheMap").Count == 0 &&
                h.LoadingNames.Count == 0 && Get<AssetBundleLoader>(h.Provider, "_bundleLoader") == null,
                "Reentrant and repeated Provider.Dispose preserve complete shutdown and empty loading registration.");
        }

        private static async UniTask SceneLifetime()
        {
            var h = New(); var sceneHandle = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive)); h.Handles.Add(sceneHandle);
            await Wait(() => h.Has("scene.ab") && h.SceneEntries().Any(entry =>
                Get<BaseTask>(entry, "_loadSceneTask") != null), "Scene task created after Bundle delivery");
            Check(!sceneHandle.IsDone, "Scene bundle reference acquired before scene completion.");
            var loadTask = (LoadSceneTask)Get<BaseTask>(h.SceneEntries().Single(), "_loadSceneTask");
            if (!loadTask.IsExecuted) loadTask.Execute();
            var loadOperation = Get<AsyncOperation>(loadTask, "_sceneRequest");
            Check(loadOperation != null && !loadOperation.isDone, "Actual Unity scene load is still in flight when cleanup is invoked.");
            h.Assets.UnloadUnusedAssets(); await Wait(() => sceneHandle.IsDone, "Scene load");
            Check(sceneHandle.Result.HasValue, "Scene Handle exposes a nullable loaded Scene.");
            Check(sceneHandle.State == AssetHandleState.Succeeded && sceneHandle.Error == null && !sceneHandle.IsDisposed,
                "Scene success is published with a valid owner and no error.");
            var scene = sceneHandle.Result.Value;
            var lease = Get<SceneInfoCache.Lease>(sceneHandle, "_lease");
            var entry = Get<SceneInfoCache>(lease, "_cache");
            Check(scene.IsValid() && scene.isLoaded && h.Refs("scene.ab") == 1 &&
                ReferenceEquals(Get<AssetBundleCache>(Get<AssetBundleCache.Lease>(entry, "_bundleLease"), "_cache"), h.Cache("scene.ab")),
                "Scene lease binds to its entry and exact Bundle cache with only one reference.");
            sceneHandle.Dispose(); sceneHandle.Dispose();
            lease.Dispose();
            Check(sceneHandle.Result == null && entry.Stage == AssetCacheStage.Unloading, "Repeated Handle and Lease release only requests one unload.");
            Check(sceneHandle.State == AssetHandleState.Succeeded && sceneHandle.IsDisposed && sceneHandle.Error == null,
                "Scene Dispose does not rewrite successful loading history.");
            var unloadTask = TaskSnapshot().OfType<AsyncOperationTask>().Last();
            var unloadOperation = Get<AsyncOperation>(unloadTask, "_asyncOperation");
            Check(unloadOperation != null && !unloadOperation.isDone, "Actual Unity scene unload is still in flight when cleanup is invoked.");
            h.Assets.UnloadUnusedAssets();
            Check(h.Has("scene.ab") && h.Refs("scene.ab") == 1, "Unload submission and duplicate Dispose retain bundle reference.");
            await Wait(() => h.Has("scene.ab") && h.Refs("scene.ab") == 0, "Scene unload reference return");
            Check(!scene.IsValid() || !scene.isLoaded, "Reference returned only after actual scene unload.");
            h.Assets.UnloadUnusedAssets(); Check(!h.Has("scene.ab") && !h.Has("b.ab"), "Scene and dependency bundles reclaimed.");
        }

        private static async UniTask SceneCancellation()
        {
            var h = New(); using var cts = new CancellationTokenSource();
            var handle = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive), ct: cts.Token); h.Handles.Add(handle);
            await Wait(() => h.Has("scene.ab") && h.SceneEntries().Any(entry =>
                Get<BaseTask>(entry, "_loadSceneTask") != null), "Scene task created before cancellation");
            var task = Get<BaseTask>(h.SceneEntries().Single(), "_loadSceneTask");
            if (!task.IsExecuted) task.Execute();
            Check(Get<AsyncOperation>(task, "_sceneRequest") is { isDone: false }, "Cancel targets an already submitted Unity scene load.");
            cts.Cancel();
            var canceled = false; try { await handle; } catch (OperationCanceledException) { canceled = true; }
            Check(canceled && h.Refs("scene.ab") == 1, "Handle cancellation retains in-flight scene reference.");
            Check(handle.State == AssetHandleState.Cancelled && handle.IsDone && !handle.IsDisposed && handle.Error == null,
                "Scene cancellation publishes its request terminal before native cleanup.");
            await Wait(() => handle.IsDone && h.Has("scene.ab") && h.Refs("scene.ab") == 0, "Late scene cleanup", 20);
            var scene = SceneManager.GetSceneByName("validation_scene");
            Check(!scene.IsValid() || !scene.isLoaded, "Late successful scene is unloaded after cancellation.");
        }

        private static async UniTask SceneFailure()
        {
            var h = New();
            for (var i = 0; i < 2; i++)
            {
                var handle = h.Assets.LoadSceneHandle("assets/__session_validation/nonexistent_scene.unity", new LoadSceneParameters(LoadSceneMode.Additive)); h.Handles.Add(handle);
                await Wait(() => handle.IsDone, "Invalid scene attempt " + i);
                Check(handle.Result == null && h.Refs("scene.ab") == 0, "Invalid scene completes with null and returns its reference, attempt " + i);
                var loading = Get<HashSet<string>>(h.Provider, "_loadingSceneSet");
                Check(loading.Count == 0, "Failed scene name registration removed, attempt " + i);
            }
        }

        private static async UniTask SceneFailureProfiler()
        {
            var h = New(true);
            const string path = ScenePath;
            var profiler = LoadProfiler.ProfilerCore.Instance;
            var wasEnabled = profiler.IsEnabled;
            profiler.SetEnabled(true);
            profiler.Clear();
            var store = Get<LoadProfiler.ProfilerRecordStore>(profiler, "_recordStore");
            var events = new List<AssetLoadEventArgs>();
            var starts = new List<LoadProfiler.ProfilerRecord>();
            void CaptureSceneEvent(AssetLoadEventArgs args)
            {
                if (args.TargetType != AssetLoadEventType.Scene || args.AssetPath != path) return;
                events.Add(args);
                if (args.Timing == AssetLoadEventTiming.Begin)
                {
                    starts.Add(Get<Dictionary<string, LoadProfiler.ProfilerRecord>>(store, "_pendingSceneRecords").Values.Single());
                }
            }
            AssetLoadEventDispatcher.OnLoadEvent += CaptureSceneEvent;
            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var success = i == 2;
                    var handle = i == 0 ? QueueScene(h) : h.Scene();
                    var entry = h.SceneEntries().Single(value => value.Stage == AssetCacheStage.Loading);
                    var task = Get<BaseTask>(entry, "_loadSceneTask");
                    Check(task.State == TaskState.Pending, "The fixture controls the Task before native scene submission.");
                    if (success) task.Execute();
                    else task.Fail(new InvalidOperationException("Controlled scene failure for Profiler validation."));
                    await Wait(() => handle.IsDone, "Scene Profiler attempt " + i);
                    Check(handle.Result.HasValue == success && h.Refs("scene.ab") == (success ? 1 : 0)
                        && (success || Get<AssetBundleCache.Lease>(entry, "_bundleLease") == null),
                        "Failure clears the Bundle Lease; success retains scene ownership.");
                    Check(h.LoadingNames.Count == 0, "Scene completion removes the loading registration.");
                    Check(events.Count == (i + 1) * 2 && starts.Count == i + 1,
                        "Each attempt emits one Scene Begin and End event.");
                    var begin = events[i * 2];
                    var end = events[i * 2 + 1];
                    Check(begin.Timing == AssetLoadEventTiming.Begin && end.Timing == AssetLoadEventTiming.End
                        && begin.BundlePath == "scene.ab" && end.BundlePath == begin.BundlePath
                        && begin.AssetType == typeof(Scene) && end.AssetType == begin.AssetType && end.IsSuccess == success,
                        "Scene failure preserves the Begin identity after releasing its Bundle Lease.");
                    var records = store.GetAll().Where(record => record.Type == AssetLoadEventType.Scene && record.AssetPath == path).ToArray();
                    Check(records.Length == i + 1 && ReferenceEquals(records[i], starts[i])
                        && records[i].IsSuccess == success && records[i].BundlePath == "scene.ab"
                        && (success || !string.IsNullOrEmpty(records[i].ErrorMessage)) && records[i].EndTimestamp >= records[i].StartTimestamp,
                        "The real Profiler completes the Scene record from this attempt with the correct outcome.");
                    Check(Get<Dictionary<string, LoadProfiler.ProfilerRecord>>(store, "_pendingSceneRecords").Count == 0,
                        "Scene completion leaves no pending Profiler record.");
                    if (i > 0)
                    {
                        Check(!ReferenceEquals(starts[i], starts[i - 1]) && records[i].RecordId != records[i - 1].RecordId,
                            "Same-path retry has a fresh record instead of reusing the previous start.");
                    }
                    handle.Dispose();
                    if (success)
                    {
                        await Wait(() => entry.Stage == AssetCacheStage.Unloaded, "Successful Profiler retry scene cleanup");
                        Check(h.Refs("scene.ab") == 0, "Successful retry releases its scene ownership normally.");
                    }
                }
            }
            finally
            {
                AssetLoadEventDispatcher.OnLoadEvent -= CaptureSceneEvent;
                profiler.Clear();
                profiler.SetEnabled(wasEnabled);
            }
        }

        private static async UniTask SceneTaskCancel()
        {
            var calls = 0; var task = new LoadSceneTask("nonexistent_scene", new LoadSceneParameters(LoadSceneMode.Additive), completed => calls++);
            task.Cancel(); task.Cancel(); task.Dispose(); Check(calls == 1 && !task.IsExecuted, "Queued scene cancellation notifies once.");
            await UniTask.Yield();
        }

        private static async UniTask ScenePreCancelled()
        {
            var h = New();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var handle = h.Scene(cts.Token);
            await ExpectCanceled(handle, "Pre-cancelled scene wait");
            Check(handle.IsDone && handle.Result == null && !h.Has("scene.ab") &&
                h.Locater.Calls.Count == 0 && h.LoadingNames.Count == 0,
                "Pre-cancelled SceneHandle creates no task, cache, registration or lease.");
        }

        private static async UniTask ExpectCanceled(SceneHandle handle, string evidence)
        {
            var canceled = false;
            try { await handle; }
            catch (OperationCanceledException) { canceled = true; }
            Check(canceled, evidence + " throws OperationCanceledException.");
        }

        private static SceneHandle QueueScene(Harness h)
        {
            var handle = h.Scene();
            h.Provider.Tick(0);
            h.Finish("b.ab", true);
            h.Finish("scene.ab", true);
            return handle;
        }

        private static async UniTask SceneDisposeDuringLoad()
        {
            var h = New(true);
            var handle = QueueScene(h);
            var entry = h.SceneEntries().Single();
            var task = Get<BaseTask>(entry, "_loadSceneTask");
            task.Execute();
            var op = Get<AsyncOperation>(task, "_sceneRequest");
            Check(op != null && !op.isDone, "Dispose occurs after Unity has accepted the scene load.");
            handle.Dispose();
            handle.Dispose();
            await ExpectCanceled(handle, "Disposed loading SceneHandle");
            Check(h.Refs("scene.ab") == 1 && entry.Stage == AssetCacheStage.Loading,
                "Handle disposal does not release an in-flight scene's Bundle lease.");
            await Wait(() => handle.IsDone && entry.Stage == AssetCacheStage.Unloaded, "Late result after Dispose", 20);
            Check(handle.Result == null && h.Refs("scene.ab") == 0 && h.LoadingNames.Count == 0,
                "Late success is unloaded and returns its lease and loading registration.");
            Check(!SceneManager.GetSceneByName("validation_scene").isLoaded,
                "Disposed loading Handle leaves no live scene instance.");
        }

        private static async UniTask SceneConcurrentGuard()
        {
            var h = New(true);
            var first = h.Scene();
            var duplicate = h.Scene();
            Check(duplicate.IsDone && duplicate.Result == null && !first.IsDone,
                "Same-name request is rejected while the first bundle is pending.");
            await Frame();
            Check(h.LoadingNames.Count == 1, "Registration survives return from the asynchronous submission method.");
            h.Finish("b.ab", true);
            h.Finish("scene.ab", true);
            var task = Get<BaseTask>(h.SceneEntries().Single(), "_loadSceneTask");
            task.Execute();
            var inFlightDuplicate = h.Scene();
            Check(inFlightDuplicate.IsDone && inFlightDuplicate.Result == null && h.Refs("scene.ab") == 1,
                "Same-name request is also rejected during actual Unity scene loading without acquiring another reference.");
            await Wait(() => first.IsDone, "Original guarded scene");
            Check(first.Result.HasValue && h.LoadingNames.Count == 0,
                "Successful completion removes the original registration.");
        }

        private static async UniTask SceneIndependentInstances()
        {
            var h = New();
            var first = h.Scene();
            await Wait(() => first.IsDone, "First independent scene");
            var second = h.Scene();
            await Wait(() => second.IsDone, "Second independent scene");
            Check(first.Result.HasValue && second.Result.HasValue, "Sequential Additive requests both succeed.");
            var firstScene = first.Result.Value;
            var secondScene = second.Result.Value;
            var firstLease = Get<SceneInfoCache.Lease>(first, "_lease");
            var secondLease = Get<SceneInfoCache.Lease>(second, "_lease");
            Check(firstScene.handle != secondScene.handle &&
                !ReferenceEquals(Get<SceneInfoCache>(firstLease, "_cache"), Get<SceneInfoCache>(secondLease, "_cache")),
                "Same path produces distinct native Scene handles and distinct SceneInfoCache owners.");
            Check(h.Refs("scene.ab") == 2 && h.Refs("b.ab") == 1,
                "Two scene entries own two main Bundle leases and one parent-owned dependency lease.");
            first.Dispose();
            firstLease.Dispose();
            first.Dispose();
            await Wait(() => !firstScene.isLoaded && h.Refs("scene.ab") == 1, "First independent unload");
            Check(secondScene.isLoaded && second.Result.HasValue, "Releasing first instance does not unload second.");
            h.Assets.UnloadUnusedAssets();
            Check(h.Has("scene.ab") && h.Has("b.ab"), "Other scene instance keeps main and dependency bundles alive.");
            second.Dispose();
            secondLease.Dispose();
            await Wait(() => !secondScene.isLoaded && h.Refs("scene.ab") == 0, "Second independent unload");
            h.Assets.UnloadUnusedAssets();
            Check(!h.Has("scene.ab") && !h.Has("b.ab"), "Last instance release permits both bundles to be reclaimed.");
        }

        private static async UniTask SceneBundleFailure()
        {
            var h = New(true);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var handle = h.Scene();
                h.Provider.Tick(0);
                h.Finish("b.ab", true);
                var original = new InvalidOperationException("Injected scene Bundle failure " + attempt);
                h.Locater.Manual["scene.ab"].Fail(original);
                h.Provider.Tick(0);
                await Wait(() => handle.IsDone, "Failed scene bundle " + attempt);
                Check(handle.Result == null && h.LoadingNames.Count == 0 && h.Refs("b.ab") == 0,
                    "Scene bundle failure clears name and rolls back dependency lease, attempt " + attempt);
                Check(h.SceneEntries().Count == 0, "A failed bundle never creates a scene entry.");
                Check(handle.State == AssetHandleState.Failed && handle.Error.ResourceKey == "scene.ab" &&
                    handle.Error.Message == original.Message && ReferenceEquals(handle.Error.Exception, original),
                    "Scene requests preserve the failed Bundle identity and original exception without a request wrapper.");
                handle.Dispose();
                handle.Dispose();
                h.Assets.UnloadUnusedAssets();
            }
        }

        private static async UniTask SceneMissingMetadata()
        {
            var h = New();
            var missing = h.Assets.LoadSceneHandle("missing/validation_scene.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            h.Handles.Add(missing);
            Check(missing.IsDone && missing.Result == null && h.LoadingNames.Count == 0,
                "Missing metadata ends the request and removes its scene-name registration.");
            var valid = h.Scene();
            await Wait(() => valid.IsDone, "Same name after missing metadata");
            Check(valid.Result.HasValue, "A valid path with the same name is not poisoned by the failed request.");
        }

        private static async UniTask ObserveScene(SceneHandle handle, Action<Scene?> completed)
        {
            await handle;
            completed(handle.Result);
        }

        private static async UniTask SceneEntryDispose()
        {
            var h = New(true);
            var handle = QueueScene(h);
            var entry = h.SceneEntries().Single();
            var task = Get<BaseTask>(entry, "_loadSceneTask");
            Check(!task.IsExecuted && !handle.IsDone, "Entry Dispose targets a queued, unsubmitted Unity task.");
            var completions = 0;
            var waiter = ObserveScene(handle, result => { completions++; Check(result == null, "Disposed entry completes waiting Handle with null."); });
            entry.Dispose();
            entry.Dispose();
            await Wait(() => handle.IsDone, "Entry Dispose completion");
            await waiter;
            Check(completions == 1 && entry.Stage == AssetCacheStage.Unloaded && task.IsDone && !task.IsExecuted,
                "Entry Dispose ends request exactly once without executing its queued Unity load.");
            Check(h.Refs("scene.ab") == 0 && h.LoadingNames.Count == 0 &&
                Get<Action<AssetLoadResult<SceneInfoCache.Lease>>>(entry, "_sceneLoaderCallback") == null,
                "Entry cleanup returns its Bundle lease, removes registration and clears callback.");
            task.Cancel();
            await Frame();
            Check(completions == 1 && !SceneManager.GetSceneByName("validation_scene").isLoaded,
                "Repeated task cancellation does not notify again or create a scene.");
        }

        private static async UniTask SceneSystemDisposeDuringBundle()
        {
            var h = New(true);
            var handle = h.Scene();
            h.Provider.Tick(0);
            Check(!handle.IsDone && h.LoadingNames.Count == 1 && h.SceneEntries().Count == 0,
                "System Dispose targets a pending Bundle request before scene entry creation.");
            var completions = 0;
            var waiter = ObserveScene(handle, result => { completions++; Check(result == null, "Pending bundle shutdown delivers failure."); });
            h.Shutdown();
            await Wait(() => handle.IsDone, "System Dispose during bundle loading");
            await waiter;
            Check(completions == 1 && h.LoadingNames.Count == 0 &&
                Get<Dictionary<string, AssetBundleCache>>(h.Provider, "_bundleCacheMap").Count == 0,
                "Shutdown ends the pending request once and clears Provider registration/cache storage.");
            handle.Dispose();
            handle.Dispose();
            Check(completions == 1, "Disposing the ended Handle cannot notify again.");
        }

        private static async UniTask SceneSystemDisposeQueued()
        {
            var h = New(true);
            var handle = QueueScene(h);
            var entry = h.SceneEntries().Single();
            var task = Get<BaseTask>(entry, "_loadSceneTask");
            var bundle = h.Cache("scene.ab");
            var completions = 0;
            var waiter = ObserveScene(handle, result => { completions++; Check(result == null, "Queued scene shutdown delivers failure."); });
            Check(!task.IsExecuted, "System Dispose occurs before the native scene load is submitted.");
            h.Shutdown();
            await Wait(() => handle.IsDone, "System Dispose with queued scene");
            await waiter;
            Check(completions == 1 && entry.Stage == AssetCacheStage.Unloaded && task.IsDone && !task.IsExecuted,
                "Parent disposal reaches scene entry and terminates the queued request once.");
            Check(bundle.Stage == AssetCacheStage.Unloaded && Get<int>(bundle, "_refCount") == 0 && h.LoadingNames.Count == 0,
                "Scene and Bundle terminal state and registration are consistent after shutdown.");
            entry.Dispose();
            handle.Dispose();
            await Frame();
            Check(completions == 1 && !SceneManager.GetSceneByName("validation_scene").isLoaded,
                "Repeated entry/Handle disposal causes no late scene creation or duplicate result.");
        }

        private static async UniTask SceneQueuedTaskCancel()
        {
            var h = New(true);
            var handle = QueueScene(h);
            var entry = h.SceneEntries().Single();
            var task = Get<BaseTask>(entry, "_loadSceneTask");
            task.Cancel();
            task.Cancel();
            await Wait(() => handle.IsDone, "Queued task cancel through scene cache");
            Check(handle.Result == null && entry.Stage == AssetCacheStage.Unloaded && h.Refs("scene.ab") == 0,
                "Pre-submission task cancellation returns null through the full scene request chain and releases its Bundle lease.");
            Check(h.LoadingNames.Count == 0 && !task.IsExecuted, "Task cancellation clears registration without starting Unity load.");
        }

        private static async UniTask WithEditorScenes(bool delayed, Func<AssetSystem, AssetEditorProvider, UniTask> action)
        {
            var setting = LiteRuntime.Setting.Asset;
            var oldDelay = setting.SimulateAsyncDelayInEditor;
            var oldMin = setting.AsyncDelayMinFrame;
            var oldMax = setting.AsyncDelayMaxFrame;
            AssetSystem assets = null;
            try
            {
                setting.SimulateAsyncDelayInEditor = delayed;
                setting.AsyncDelayMinFrame = 59;
                setting.AsyncDelayMaxFrame = 60;
                var provider = new AssetEditorProvider();
                Check(await provider.Initialize(), "Editor scene provider initializes; simulated delay=" + delayed);
                assets = new AssetSystem();
                Set(assets, "_provider", provider);
                await action(assets, provider);
            }
            finally
            {
                assets?.Dispose();
                setting.SimulateAsyncDelayInEditor = oldDelay;
                setting.AsyncDelayMinFrame = oldMin;
                setting.AsyncDelayMaxFrame = oldMax;
            }
        }

        private static SceneHandle EditorScene(AssetSystem assets, string path = EditorScenePath, CancellationToken token = default)
        {
            var handle = assets.LoadSceneHandle(path, new LoadSceneParameters(LoadSceneMode.Additive), ct: token);
            Current.Handles.Add(handle);
            return handle;
        }

        private static async UniTask EditorSceneLifetime(bool delayed)
        {
            New();
            await WithEditorScenes(delayed, async (assets, provider) =>
            {
                var first = EditorScene(assets);
                await Wait(() => first.IsDone, "First Editor scene");
                var second = EditorScene(assets);
                await Wait(() => second.IsDone, "Second Editor scene");
                Check(first.Result.HasValue && second.Result.HasValue, "Both Editor loads produce live scenes.");
                var a = first.Result.Value;
                var b = second.Result.Value;
                var lease = Get<SceneInfoCache.Lease>(first, "_lease");
                var entry = Get<SceneInfoCache>(lease, "_cache");
                Check(a.handle != b.handle && entry.Stage == AssetCacheStage.Loaded &&
                    Get<AssetBundleCache.Lease>(entry, "_bundleLease") == null,
                    "Editor uses independent native instances and a Loaded scene entry without Bundle lease.");
                first.Dispose();
                lease.Dispose();
                first.Dispose();
                Check(first.Result == null && entry.Stage == AssetCacheStage.Unloading,
                    "Editor repeated release hides result and enters the common async unload lifecycle.");
                await Wait(() => entry.Stage == AssetCacheStage.Unloaded && !a.isLoaded, "Editor first instance unload");
                Check(b.isLoaded && second.Result.HasValue, "Second Editor instance survives first instance disposal.");
                second.Dispose();
                second.Dispose();
                await Wait(() => !b.isLoaded, "Editor second instance unload");
                Check(Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0, "Editor scene names are no longer registered.");
            });
        }

        private static async UniTask EditorSceneCancellation(bool delayed)
        {
            New();
            await WithEditorScenes(delayed, async (assets, provider) =>
            {
                using var cts = new CancellationTokenSource();
                var canceled = EditorScene(assets, EditorScenePath, cts.Token);
                var disposed = EditorScene(assets, EditorSecondScenePath);
                cts.Cancel();
                disposed.Dispose();
                disposed.Dispose();
                await ExpectCanceled(canceled, "Editor token cancellation");
                await ExpectCanceled(disposed, "Editor pending Handle disposal");
                await Wait(() => Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0 &&
                    !SceneManager.GetSceneByName("editor_validation_scene").isLoaded &&
                    !SceneManager.GetSceneByName("editor_validation_second").isLoaded, "Editor late scene cleanup", 20);
                Check(canceled.Result == null && disposed.Result == null &&
                    Get<SceneInfoCache.Lease>(canceled, "_lease") == null &&
                    Get<SceneInfoCache.Lease>(disposed, "_lease") == null,
                    "Both late Editor results are released rather than retained by ended Handles.");
                Check(Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0,
                    "Editor cancellation and Dispose remove both pending name registrations.");
            });
        }

        private static async UniTask EditorSceneFailure()
        {
            New();
            await WithEditorScenes(false, async (assets, provider) =>
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();
                var count = SceneManager.sceneCount;
                var handle = EditorScene(assets, EditorScenePath, cts.Token);
                await ExpectCanceled(handle, "Editor pre-cancellation");
                Check(SceneManager.sceneCount == count && Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0,
                    "Pre-cancellation starts no native Editor load or name registration.");
                for (var i = 0; i < 2; i++)
                {
                    var failed = false;
                    try
                    {
                        var missing = EditorScene(assets, "Assets/StandaloneAssets/_session_validation/editor_validation_missing.unity");
                        await Wait(() => missing.IsDone, "Missing Editor scene");
                        failed = missing.Result == null;
                    }
                    catch (ArgumentException) { failed = true; }
                    Check(failed && Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0,
                        "Invalid Editor scene request clears its name and can be retried, attempt " + i);
                }
            });
        }

        private static async UniTask EditorSceneInvalidated()
        {
            New();
            await WithEditorScenes(true, async (assets, provider) =>
            {
                var handle = EditorScene(assets);
                Check(!handle.IsDone, "Editor request must remain pending before native completion and delayed delivery.");
                await Wait(() => SceneManager.GetSceneByName("editor_validation_scene").isLoaded, "Native scene before delayed delivery");
                var scene = SceneManager.GetSceneByName("editor_validation_scene");
                Check(!handle.IsDone, "Native Editor scene exists while public result is still delayed.");
                var operation = SceneManager.UnloadSceneAsync(scene);
                Check(operation != null, "Explicit external unload was submitted.");
                await Wait(() => operation.isDone, "External unload before Editor delivery");
                Check(!handle.IsDone, "External unload completed before the simulated delivery deadline.");
                await Wait(() => handle.IsDone, "Invalid Editor result delivery");
                Check(handle.Result == null && Get<SceneInfoCache.Lease>(handle, "_lease") == null &&
                    Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0,
                    "Delivery-time validation rejects invalid scene, releases its lease and clears its name.");
            });
        }

        private static async UniTask EditorNativeTiming()
        {
            New();
            var scene = EditorSceneManager.LoadSceneInPlayMode(EditorScenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            _case.evidence.Add("Immediately after native call: IsValid=" + scene.IsValid() + ", isLoaded=" + scene.isLoaded + ".");
            Check(scene.IsValid(), "The native Editor API returns a valid scene identity.");
            await Wait(() => scene.isLoaded, "Native Editor scene completion");
            Check(scene.isLoaded, "The same scene identity becomes loaded after player-loop progress.");
            var op = SceneManager.UnloadSceneAsync(scene);
            Check(op != null, "Timing probe can explicitly unload its fixture.");
            await Wait(() => op.isDone && !scene.isLoaded, "Native timing probe cleanup");
        }

        private static void RestoreValidationHost()
        {
            if (!SceneManager.GetSceneByName("SessionValidationHost").isLoaded)
            {
                SceneManager.CreateScene("SessionValidationHost");
            }
        }

        private static async UniTask SceneAutomaticUnload(bool single)
        {
            var h = New();
            try
            {
                var first = h.Scene();
                await Wait(() => first.IsDone, "First Bundle scene");
                Check(first.Result.HasValue, "First scene loaded.");
                var firstScene = first.Result.Value;
                var firstLease = Get<SceneInfoCache.Lease>(first, "_lease");
                var firstEntry = Get<SceneInfoCache>(firstLease, "_cache");
                var bundle = h.Cache("scene.ab");
                var second = h.Assets.LoadSceneHandle(ScenePath,
                    new LoadSceneParameters(single ? LoadSceneMode.Single : LoadSceneMode.Additive));
                h.Handles.Add(second);
                await Wait(() => second.IsDone, "Second Bundle scene");
                Check(second.Result.HasValue && second.Result.Value.handle != firstScene.handle,
                    "Second load creates a distinct live scene instance.");
                if (single)
                {
                    RestoreValidationHost();
                }
                else
                {
                    Check(h.Refs("scene.ab") == 2, "Both loaded scenes retain one bundle reference each.");
                    var op = SceneManager.UnloadSceneAsync(firstScene);
                    Check(op != null, "External native unload was submitted without disposing the Handle.");
                    await Wait(() => op.isDone, "External Bundle scene unload");
                }

                await Wait(() => firstEntry.Stage == AssetCacheStage.Unloaded, "Automatic Bundle entry completion");
                Check(!Get<bool>(first, "_isDisposed") && first.Result == null && !firstScene.isLoaded,
                    "Native unload ends the entry before the original Handle is disposed.");
                Check(ReferenceEquals(bundle, h.Cache("scene.ab")) && h.Refs("scene.ab") == 1,
                    "Only the old scene reference is returned to the same shared bundle.");
                first.Dispose(); first.Dispose(); firstLease.Dispose(); firstEntry.Dispose();
                await Frame();
                Check(h.Refs("scene.ab") == 1 && second.Result.HasValue && second.Result.Value.isLoaded,
                    "Repeated old Handle, Lease and entry disposal preserves the surviving scene.");
                Check(h.LoadingNames.Count == 0 && !h.SceneEntries().Contains(firstEntry),
                    "Loading registrations are clear and Tick removes the completed entry.");
                second.Dispose(); second.Dispose();
                await Wait(() => h.Refs("scene.ab") == 0, "Final Bundle scene reference return");
                h.Assets.UnloadUnusedAssets(20);
                Check(!h.Has("scene.ab") && !h.Has("b.ab"), "Scene bundle and dependency are reclaimed.");
            }
            finally
            {
                // Single removes the original host. Keep a scene for normal fixture cleanup.
                if (single) RestoreValidationHost();
            }
        }

        private static async UniTask EditorSceneAutomaticUnload(bool single, bool delayed)
        {
            New();
            await WithEditorScenes(delayed, async (assets, provider) =>
            {
                try
                {
                    var first = EditorScene(assets);
                    await Wait(() => first.IsDone, "First Editor scene");
                    Check(first.Result.HasValue, "First Editor scene loaded.");
                    var firstScene = first.Result.Value;
                    var firstLease = Get<SceneInfoCache.Lease>(first, "_lease");
                    var firstEntry = Get<SceneInfoCache>(firstLease, "_cache");
                    var second = assets.LoadSceneHandle(single ? EditorSecondScenePath : EditorScenePath,
                        new LoadSceneParameters(single ? LoadSceneMode.Single : LoadSceneMode.Additive));
                    Current.Handles.Add(second);
                    await Wait(() => second.IsDone, "Second Editor scene");
                    Check(second.Result.HasValue && second.Result.Value.handle != firstScene.handle,
                        "Second Editor load creates a distinct live scene instance.");
                    if (single)
                    {
                        RestoreValidationHost();
                    }
                    else
                    {
                        var op = SceneManager.UnloadSceneAsync(firstScene);
                        Check(op != null, "External Editor scene unload was submitted.");
                        await Wait(() => op.isDone, "External Editor scene unload");
                    }

                    await Wait(() => firstEntry.Stage == AssetCacheStage.Unloaded, "Automatic Editor entry completion");
                    Check(!Get<bool>(first, "_isDisposed") && first.Result == null && !firstScene.isLoaded &&
                        Get<AssetBundleCache.Lease>(firstEntry, "_bundleLease") == null,
                        "Editor uses common automatic completion without a Bundle lease or Handle disposal.");
                    first.Dispose(); first.Dispose(); firstLease.Dispose(); firstEntry.Dispose();
                    await Frame();
                    Check(second.Result.HasValue && second.Result.Value.isLoaded,
                        "Repeated old releases preserve the surviving Editor scene.");
                    Check(Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0,
                        "Editor loading registrations are clear.");
                    var secondScene = second.Result.Value;
                    var secondEntry = Get<SceneInfoCache>(Get<SceneInfoCache.Lease>(second, "_lease"), "_cache");
                    second.Dispose(); second.Dispose();
                    await Wait(() => secondEntry.Stage == AssetCacheStage.Unloaded && !secondScene.isLoaded,
                        "Final Editor scene release");
                    Check(second.Result == null, "Final Editor Handle releases normally after automatic old-scene cleanup.");
                }
                finally
                {
                    if (single) RestoreValidationHost();
                }
            });
        }

        private static async UniTask SceneUnloadRetry()
        {
            var h = New();
            var handle = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            h.Handles.Add(handle);
            var scene = default(Scene);
            try
            {
                await Wait(() => handle.IsDone, "Single scene before unload failure");
                Check(handle.Result.HasValue && SceneManager.sceneCount == 1, "The fixture is the only loaded ordinary scene.");
                scene = handle.Result.Value;
                var lease = Get<SceneInfoCache.Lease>(handle, "_lease");
                var entry = Get<SceneInfoCache>(lease, "_cache");
                handle.Dispose();
                _case.evidence.Add($"After failed unload: scene loaded={scene.isLoaded}, entry={entry.Stage}, Handle lease cleared={Get<SceneInfoCache.Lease>(handle, "_lease") == null}, Lease cache cleared={Get<SceneInfoCache>(lease, "_cache") == null}.");
                Check(scene.isLoaded && h.Refs("scene.ab") == 1, "A failed unload keeps the still-live scene's Bundle pinned.");
                Check(entry.Stage == AssetCacheStage.Unloading && Get<AsyncOperation>(entry, "_unloadOperation") == null,
                    "A refused native unload has no active operation and remains eligible for an explicit retry.");
                handle.Dispose(); lease.Dispose();
                Check(h.Refs("scene.ab") == 1, "Repeated release of the old Handle or Lease does not revive ownership or drop its protection.");
                RestoreValidationHost();
                entry.Unload();
                var retryOperation = Get<AsyncOperation>(entry, "_unloadOperation");
                Check(retryOperation != null && !retryOperation.isDone, "The explicit retry submits a real native unload.");
                entry.Unload();
                Check(ReferenceEquals(retryOperation, Get<AsyncOperation>(entry, "_unloadOperation")),
                    "Repeated requests during native unloading reuse the same operation.");
                var deadline = Time.realtimeSinceStartup + 0.2f;
                while (scene.IsValid() && scene.isLoaded && Time.realtimeSinceStartup < deadline) await Frame();
                _case.evidence.Add($"After restoring a host and retrying the entry: stage={entry.Stage}, scene loaded={scene.isLoaded}.");
                Check(!scene.IsValid() || !scene.isLoaded, "Once unloading is legal, the scene entry must accept an unload retry.");
                await Wait(() => retryOperation.isDone && entry.Stage == AssetCacheStage.Unloaded, "Scene retry completion notification");
                Check(entry.Stage == AssetCacheStage.Unloaded && h.Refs("scene.ab") == 0
                    && Get<AsyncOperation>(entry, "_unloadOperation") == null,
                    "Successful retry returns the Bundle reference and clears its operation.");
            }
            finally
            {
                RestoreValidationHost();
                if (scene.IsValid() && scene.isLoaded)
                {
                    _case.evidence.Add("Harness-only cleanup of the scene left after a refused unload retry; the production failure remains recorded.");
                    var operation = SceneManager.UnloadSceneAsync(scene);
                    if (operation != null) await Wait(() => operation.isDone, "Scene retry fixture isolation");
                }
            }
        }

        private static async UniTask SceneObserverFailure()
        {
            var h = New(); var handle = h.Scene();
            await Wait(() => handle.IsDone, "Scene before observer registration failure");
            var scene = handle.Result.Value;
            var entry = Get<SceneInfoCache>(Get<SceneInfoCache.Lease>(handle, "_lease"), "_cache");
            var bundleLease = Get<AssetBundleCache.Lease>(entry, "_bundleLease");
            var groups = Get<List<TaskGroup>>(LiteRuntime.Task, "_groups");
            var flowIndex = groups.FindIndex(group => group.Id == LiteConst.TaskGroupId.Flow);
            var flow = groups[flowIndex];
            AsyncOperation operation = null;
            try
            {
                groups.RemoveAt(flowIndex);
                handle.Dispose();
                operation = Get<AsyncOperation>(entry, "_unloadOperation");
                Check(operation != null && !operation.isDone && entry.Stage == AssetCacheStage.Unloading && h.Refs("scene.ab") == 1,
                    "Observer rejection does not discard the submitted native operation or release the scene protection.");
                entry.Unload();
                Check(ReferenceEquals(operation, Get<AsyncOperation>(entry, "_unloadOperation")),
                    "An active native unload cannot be submitted twice after its observer registration failed.");
                Check(!TaskSnapshot().OfType<AsyncOperationTask>().Any(task => ReferenceEquals(Get<AsyncOperation>(task, "_asyncOperation"), operation)),
                    "The failure fixture has no registered unload observer.");
            }
            finally
            {
                groups.Insert(flowIndex, flow);
            }
            await Wait(() => entry.Stage == AssetCacheStage.Unloaded, "Unobserved native scene unload");
            Check(operation.isDone && (!scene.IsValid() || !scene.isLoaded) && h.Refs("scene.ab") == 0
                && Get<AsyncOperation>(entry, "_unloadOperation") == null,
                "The existing sceneUnloaded event completes cleanup independently of the missing observer.");
            typeof(SceneInfoCache).GetMethod("OnSceneUnloaded", Private).Invoke(entry, new object[] { scene });
            bundleLease.Dispose(); entry.Unload(); handle.Dispose();
            Check(h.Refs("scene.ab") == 0, "Duplicate scene notifications and releases cannot decrement the Bundle reference twice.");
        }

        private static async UniTask SceneExternalUnloadAfterRefusal()
        {
            var h = New();
            var handle = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            h.Handles.Add(handle);
            var scene = default(Scene);
            try
            {
                await Wait(() => handle.IsDone, "Single scene before external recovery");
                scene = handle.Result.Value;
                var entry = Get<SceneInfoCache>(Get<SceneInfoCache.Lease>(handle, "_lease"), "_cache");
                handle.Dispose();
                Check(scene.isLoaded && Get<AsyncOperation>(entry, "_unloadOperation") == null && h.Refs("scene.ab") == 1,
                    "Refused unloading retains the scene and Bundle reference without a native operation.");
                RestoreValidationHost();
                var operation = SceneManager.UnloadSceneAsync(scene);
                Check(operation != null, "External recovery submits an actual scene unload after restoring a host.");
                await Wait(() => entry.Stage == AssetCacheStage.Unloaded, "External recovery after refused unload");
                Check((!scene.IsValid() || !scene.isLoaded) && h.Refs("scene.ab") == 0 && handle.Result == null,
                    "External unload returns ownership once without reviving the released Handle.");
            }
            finally
            {
                RestoreValidationHost();
                if (scene.IsValid() && scene.isLoaded)
                {
                    _case.evidence.Add("Harness-only isolation of a scene left after failed external recovery.");
                    var operation = SceneManager.UnloadSceneAsync(scene);
                    if (operation != null) await Wait(() => operation.isDone, "External recovery fixture isolation");
                }
            }
        }
    }
}
