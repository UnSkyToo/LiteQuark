using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal sealed partial class AssetLifecycleValidationRunner : BundleValidation
    {
        private static SceneHandle QueueScene(Harness h)
        {
            var handle = h.Scene();
            h.Provider.Tick(0);
            h.Finish("b.ab", true);
            h.Finish("scene.ab", true);
            return handle;
        }

        private static async UniTask ShutdownWaitsForNativeScene()
        {
            var h = New(true);
            var handle = QueueScene(h);
            var entry = h.SceneEntries().Single();
            var task = (LoadSceneTask)Get<BaseTask>(entry, "_loadSceneTask");
            var bundle = NativeBundle(h.Cache("scene.ab").Bundle);
            var dependency = NativeBundle(h.Cache("b.ab").Bundle);
            task.Execute();
            var operation = Get<AsyncOperation>(task, "_sceneRequest");
            Check(operation != null && !operation.isDone, "System close targets an actual native scene load.");
            var shutdown = h.Shutdown();
            task.Dispose();
            Check(shutdown.Status == UniTaskStatus.Pending && task.State == TaskState.Cancelled && handle.IsDone && bundle != null && dependency != null,
                "Closing ends the pending Handle while keeping its native loading dependencies alive.");
            await shutdown;
            Check(operation.isDone && task.WaitForCleanupAsync().Status == UniTaskStatus.Succeeded &&
                !SceneManager.GetSceneByName("validation_scene").isLoaded && bundle == null && dependency == null,
                "System close waits for the cancelled task's late Scene cleanup before unloading either Bundle.");
        }

        private static async UniTask EditorShutdownScene()
        {
            New();
            await WithSimulateScenes(async (assets, provider) =>
            {
                var handle = EditorScene(assets);
                // Start the queued native operation explicitly to close before the player loop completes it.
                await Wait(() => TaskSnapshot().OfType<LoadEditorSceneTask>().Any(), "Simulate scene task admission");
                var task = TaskSnapshot().OfType<LoadEditorSceneTask>().Single();
                task.Execute();
                var operation = (AsyncOperation)typeof(LoadSceneTask).GetField("_sceneRequest", Private).GetValue(task);
                Check(operation != null && !operation.isDone && !handle.IsDone,
                    "Close targets an actual native Editor scene load before result delivery.");
                var shutdown = assets.DisposeAsync();
                await shutdown;
                Check(handle.State == AssetHandleState.Failed &&
                    ((IResourceTaskCleanup)task).WaitForCleanupAsync().Status == UniTaskStatus.Succeeded &&
                    !Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(scene => scene.path == EditorScenePath),
                    "Simulate close waits for the late native scene to unload.");
            });
        }

        private static SceneHandle EditorScene(AssetSystem assets, string path = EditorScenePath, CancellationToken token = default)
        {
            var handle = assets.LoadSceneHandle(path, new LoadSceneParameters(LoadSceneMode.Additive), ct: token);
            Current.Handles.Add(handle);
            return handle;
        }

        private static async UniTask WithSimulateScenes(Func<AssetSystem, AssetManager, UniTask> action)
        {
            var setting = LiteRuntime.Setting.Asset;
            var oldLocater = setting.BundleMode;
            AssetSystem assets = null;
            try
            {
                setting.BundleMode = BundleLoadMode.Simulate;
                assets = new AssetSystem();
                var provider = (AssetManager)Get<IAssetManager>(assets, "_manager");
                _simulateManager = provider;
                // The fixture Runtime already owns the Asset group.
                Check(await provider.Initialize((IBundleProvider)typeof(AssetSystem).GetMethod("CreateBundleProvider", Private).Invoke(assets, new object[] { true })), "Simulate initializes from collected project assets.");
                await action(assets, provider);
            }
            finally
            {
                try { if (assets != null) await assets.DisposeAsync(); }
                finally
                {
                    _simulateManager = null;
                    setting.BundleMode = oldLocater;
                }
            }
        }

        private static async UniTask ShutdownReportsSceneRefusal()
        {
            var h = New();
            var handle = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            h.Handles.Add(handle);
            await Wait(() => handle.IsDone, "Single Scene before close");
            var scene = handle.Result.Value;
            var bundle = NativeBundle(h.Cache("scene.ab").Bundle);
            var asset = h.Asset<TextAsset>(TextPath);
            await Wait(() => asset.IsDone, "Independent asset before close");
            var entry = h.Entry("text.ab", TextPath);
            var pending = new CleanupProbe();
            Set(entry, "_loadAssetTask", pending);
            try
            {
                Exception failure = null;
                try { await h.Shutdown(); } catch (Exception ex) { failure = ex; }
                Check(pending.State == TaskState.Cancelled && entry.Stage == AssetCacheStage.Unloading,
                    "Shutdown reports the scene failure while another started cleanup is still pending.");
                Check(failure is InvalidOperationException && failure.Message.Contains("Scene has not been unloaded") &&
                    scene.isLoaded && bundle != null && !h.Disposed && Get<IAssetManager>(h.Assets, "_manager") != null,
                    "Refused native scene unloading fails shutdown without destroying its Bundle or clearing the Provider.");
            }
            finally
            {
                pending.FinishCleanup();
                RestoreValidationHost();
                if (scene.IsValid() && scene.isLoaded) await SceneManager.UnloadSceneAsync(scene).ToUniTask();
            }
        }

        private static void RestoreValidationHost()
        {
            if (!SceneManager.GetSceneByName("SessionValidationHost").isLoaded)
            {
                SceneManager.CreateScene("SessionValidationHost");
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
            Check(valid.Result.HasValue && valid.Result.Value.path.Equals(ScenePath, StringComparison.OrdinalIgnoreCase) &&
                valid.Result.Value.GetRootGameObjects().Any(go => go.name == "SessionValidationSceneMarker"),
                "Retry selects the expected native scene path and content.");
        }

        private sealed class CleanupProbe : BaseTask, IResourceTaskCleanup
        {
            private readonly UniTaskCompletionSource _cleanup = new();
            public UniTask WaitForCleanupAsync() => _cleanup.Task;
            internal void FinishCleanup() => _cleanup.TrySetResult();
            protected override void OnExecute() { }
        }

        private static async UniTask QueuedCancel()
        {
            var h = New(true);
            var cancelled = h.Scene(new CancellationToken(true));
            var observed = false;
            try { await cancelled; } catch (OperationCanceledException) { observed = true; }
            Check(observed && h.Locater.Calls.Count == 0, "Pre-cancelled scene starts no Bundle work.");
            var queued = QueueScene(h);
            var task = Get<BaseTask>(h.SceneEntries().Single(), "_loadSceneTask");
            await h.Shutdown();
            Check(queued.IsDone && queued.Result == null && !task.IsExecuted && !SceneManager.GetSceneByName("validation_scene").isLoaded,
                "Closing before admission ends the Handle without starting a native scene load.");
        }

        private static async UniTask SceneInstances()
        {
            var h = New();
            var first = h.Scene();
            var duplicate = h.Assets.LoadSceneHandle(ScenePath.ToUpperInvariant(), new LoadSceneParameters(LoadSceneMode.Additive));
            h.Handles.Add(duplicate);
            Check(duplicate.State == AssetHandleState.Failed, "A concurrent alias is rejected.");
            await Wait(() => first.IsDone, "First instance");
            var second = h.Scene();
            await Wait(() => second.IsDone, "Second instance");
            var a = first.Result.Value;
            var b = second.Result.Value;
            Check(a.handle != b.handle, "Sequential loads create independent native scenes.");
            await SceneManager.UnloadSceneAsync(a).ToUniTask();
            await Wait(() => !first.Result.HasValue, "External unload");
            first.Dispose();
            Check(b.isLoaded && second.Result.HasValue, "External unload leaves the other instance intact.");
            try
            {
                var replacement = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                h.Handles.Add(replacement);
                await Wait(() => replacement.IsDone, "Single scene replacement");
                Check(replacement.Result.HasValue && !b.isLoaded && !second.Result.HasValue, "Single replacement invalidates only the old instances.");
            }
            finally { RestoreValidationHost(); }
        }
    }
}
