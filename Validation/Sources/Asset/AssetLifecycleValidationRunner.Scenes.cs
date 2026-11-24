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
        private static async UniTask SubmittedScene(bool closeFromTaskContinuation)
        {
            var h = New(true);
            var handle = h.Scene();
            h.Provider.Tick(0);
            h.Finish("b.ab", true);
            h.Finish("scene.ab", true);
            var task = TaskSnapshot().OfType<LoadSceneTask>().Single();
            var bundle = NativeBundle(h.Cache("scene.ab").Bundle);
            var dependency = NativeBundle(h.Cache("b.ab").Bundle);
            task.Execute();
            var operation = Get<AsyncOperation>(task, "_sceneRequest");
            Check(operation != null && !operation.isDone, "Unity has accepted the native scene load.");
            using var duplicate = h.Assets.LoadSceneHandle(ScenePath);
            var scenes = h.Assets.GetVisitorInfo().BundleVisitors.Single(bundle => bundle.BundlePath == "scene.ab").SceneVisitors;
            Check(duplicate.State == AssetHandleState.Failed && scenes.Length == 1 && scenes[0].SceneName == ScenePath,
                "A loading scene is visible in the debugger and rejects a duplicate request.");
            if (closeFromTaskContinuation)
            {
                await task.Task;
                Check(task.State == TaskState.Succeeded && !handle.IsDone, "The native task succeeded before delivery to the scene Handle.");
            }

            var awaitingScene = handle.GetAwaiter();
            var shutdown = h.Shutdown();
            var error = handle.Error;
            Check(handle.State == AssetHandleState.Failed && error?.ResourceKey == ScenePath && awaitingScene.IsCompleted &&
                operation.isDone == closeFromTaskContinuation && shutdown.Status == UniTaskStatus.Pending && bundle != null && dependency != null,
                "Shutdown ends the Handle wait immediately and keeps the Bundles until native cleanup finishes.");
            await shutdown;
            Check(operation.isDone && ReferenceEquals(handle.Error, error) && !handle.Result.HasValue &&
                !SceneManager.GetSceneByName("validation_scene").isLoaded && bundle == null && dependency == null,
                "Shutdown settles an undelivered scene and releases its scene Bundle and dependency.");
        }

        private static async UniTask RestartWithScene()
        {
            var runtime = LiteRuntime.Instance;
            var original = LiteRuntime.Asset;
            var launcher = runtime.Launcher;
            var startup = SceneManager.GetActiveScene();
            Check(original.HasAsset(startup.path), "The fixture manifest contains the active startup scene.");
            using var duplicateStartup = original.LoadSceneHandle(startup.path.ToUpperInvariant());
            await Wait(() => duplicateStartup.IsDone, "Duplicate startup scene request");
            Check(duplicateStartup.State == AssetHandleState.Failed, "The startup scene cannot become a managed gameplay scene.");

            using var handle = original.LoadSceneHandle(EditorScenePath);
            await Wait(() => handle.IsDone, "Additive gameplay scene");
            var scene = handle.Result.Value;
            runtime.Restart();
            await Wait(() => runtime.State == LiteRuntime.RuntimeState.Running &&
                !ReferenceEquals(original, LiteRuntime.Asset), "Restart while gameplay is loaded");
            Check(launcher == runtime.Launcher && startup.isLoaded && scene.isLoaded,
                "Restart reuses the Launcher and leaves the delivered scene to its owner.");

            using var duplicate = LiteRuntime.Asset.LoadSceneHandle(EditorScenePath);
            await Wait(() => duplicate.IsDone, "Cross-generation duplicate scene");
            Check(duplicate.State == AssetHandleState.Failed, "The previous scene must exit before the same path can load again.");
            handle.Dispose();
            await Wait(() => !SceneManager.GetSceneByPath(EditorScenePath).IsValid(), "Old Handle finishes unloading its scene");
            using var next = LiteRuntime.Asset.LoadSceneHandle(EditorScenePath);
            await Wait(() => next.IsDone, "Scene reload after its owner releases it");
            Check(next.Result.HasValue, "Released scene path reloads: " + next.Error?.Message);
            var reloaded = next.Result.Value;
            try
            {
                await runtime.Shutdown();
                Check(runtime.State == LiteRuntime.RuntimeState.Idle && reloaded.isLoaded,
                    "Explicit Shutdown also completes with a delivered scene still loaded.");
                next.Dispose();
                await Wait(() => !reloaded.isLoaded, "Owner unloads its scene after Shutdown");
            }
            finally
            {
                if (runtime.State == LiteRuntime.RuntimeState.Idle) runtime.Startup(launcher);
            }
            await Wait(() => runtime.State == LiteRuntime.RuntimeState.Running, "Fixture Runtime is running");
        }

        private static async UniTask SceneFailureRetry()
        {
            var h = New(true);
            var missing = h.Assets.LoadSceneHandle("missing/validation_scene.unity");
            h.Handles.Add(missing);
            Check(missing.State == AssetHandleState.Failed && missing.Result == null,
                "A scene outside the manifest reserves no path.");
            var first = h.Scene();
            async UniTask<SceneHandle> RetryOnFailure()
            {
                await first;
                return h.Scene();
            }
            var retry = RetryOnFailure();
            h.Provider.Tick(0);
            h.Finish("b.ab", true);
            h.Finish("scene.ab", false);
            var valid = await retry;
            Check(first.State == AssetHandleState.Failed && !valid.IsDone,
                "The failure continuation can immediately reserve the same path again.");
            h.Provider.Tick(0);
            h.Finish("scene.ab", true);
            await Wait(() => valid.IsDone, "Same path after Bundle failure");
            Check(valid.Result.HasValue && valid.Result.Value.path.Equals(ScenePath, StringComparison.OrdinalIgnoreCase) &&
                valid.Result.Value.GetRootGameObjects().Any(go => go.name == "SessionValidationSceneMarker"),
                "Retry loads the expected native scene.");
            var scene = valid.Result.Value;
            valid.Dispose();
            using var unloading = h.Assets.LoadSceneHandle(ScenePath);
            Check(unloading.State == AssetHandleState.Failed, "The path stays occupied while the Handle is unloading its scene.");
            await Wait(() => !scene.isLoaded && h.Refs("scene.ab") == 0, "Retried scene exits and releases its Bundle lease");
            using var missingInBundle = h.Assets.LoadSceneHandle("Assets/__session_validation/nonexistent_scene.unity");
            Check(missingInBundle.State == AssetHandleState.Failed && missingInBundle.Result == null && h.Refs("scene.ab") == 0,
                "A missing scene path fails the request and releases its Bundle lease.");
        }

        private static async UniTask QueuedCancel()
        {
            var beforeDelivery = New(true);
            using var missing = beforeDelivery.Assets.LoadSceneHandle("Assets/__session_validation/nonexistent_scene.unity");
            var pending = beforeDelivery.Scene();
            async UniTask CloseOnFailure()
            {
                await missing;
                await beforeDelivery.Shutdown();
            }
            var closing = CloseOnFailure();
            beforeDelivery.Provider.Tick(0);
            beforeDelivery.Finish("b.ab", true);
            beforeDelivery.Finish("scene.ab", true);
            await closing;
            Check(missing.State == AssetHandleState.Failed && pending.State == AssetHandleState.Failed &&
                !SceneManager.GetSceneByName("validation_scene").isLoaded,
                "Closing from the first delivery fails the other reservation without starting a scene read.");
            await Cleanup();

            var h = New(true);
            var cancelled = h.Scene(new CancellationToken(true));
            var observed = false;
            try { await cancelled; } catch (OperationCanceledException) { observed = true; }
            Check(observed && h.Locater.Calls.Count == 0, "Pre-cancelled scene starts no Bundle work.");
            var queued = h.Scene();
            queued.Dispose();
            using var duplicate = h.Assets.LoadSceneHandle(ScenePath);
            Check(duplicate.State == AssetHandleState.Failed && h.Locater.Calls.Count == 0,
                "The cancelled consumer leaves its queued scene path reserved.");
            h.Provider.Tick(0);
            h.Finish("b.ab", true);
            h.Finish("scene.ab", true);
            var task = TaskSnapshot().OfType<LoadSceneTask>().Single();
            await h.Shutdown();
            Check(!task.IsExecuted && task.State == TaskState.Cancelled && queued.State == AssetHandleState.Cancelled &&
                !SceneManager.GetSceneByName("validation_scene").isLoaded, "Closing cancels a queued task before it reaches Unity.");
        }

        private static async UniTask ScenePathOwnership()
        {
            var h = New();
            var first = h.Scene();
            SceneVisitorInfo[] SceneVisitors() => h.Assets.GetVisitorInfo().BundleVisitors.Single(bundle => bundle.BundlePath == "scene.ab").SceneVisitors;
            void CheckDuplicate(string stage)
            {
                using var duplicate = h.Assets.LoadSceneHandle(ScenePath.ToUpperInvariant());
                Check(duplicate.State == AssetHandleState.Failed && duplicate.Error.ResourceKey == ScenePath,
                    "Duplicate path rejected while " + stage);
            }
            CheckDuplicate("waiting for the Bundle");
            Check(SceneVisitors().Length == 0, "The debugger skips the scene while its Bundle is queued.");
            await Wait(() => first.IsDone, "First scene load");
            var original = first.Result.Value;
            var scenes = SceneVisitors();
            Check(scenes.Length == 1 && scenes[0].SceneName == ScenePath, "The debugger lists the managed path under its Bundle.");
            CheckDuplicate("loaded");
            await SceneManager.UnloadSceneAsync(original).ToUniTask();
            Check(SceneVisitors().Length == 0, "The debugger hides the scene after native unload.");
            var replacement = h.Scene();
            await Wait(() => replacement.IsDone, "Reload after external scene unload");
            var current = replacement.Result.Value;
            Check(!first.Result.HasValue, "The old Handle remains empty after the same path loads again.");
            first.Dispose();
            Check(replacement.Result == current && current.isLoaded && AssetPath.ResolveScene(current.path) == ScenePath,
                "Disposing the old Handle leaves the replacement scene loaded at the requested path.");
            var main = NativeBundle(h.Cache("scene.ab").Bundle);
            var dependency = NativeBundle(h.Cache("b.ab").Bundle);
            await h.Shutdown();
            Check(main == null && dependency == null, "Closing releases both native Bundles despite the outstanding scene Handle.");
            if (current.isLoaded) await SceneManager.UnloadSceneAsync(current).ToUniTask();
            Check(!replacement.Result.HasValue, "An externally unloaded scene is no longer returned by its old Handle.");
        }
    }
}
