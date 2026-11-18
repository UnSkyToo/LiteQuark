using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Editor
{
    internal sealed partial class AssetLifecycleValidationRunner : BundleValidation
    {
        private static async UniTask PreloadErrorProtocol()
        {
            var h = New();
            var invalid = false;
            try { using var batch = h.Assets.PreloadAssetHandle<TextAsset>(new[] { TextPath, "../invalid.txt" }); }
            catch (ArgumentException) { invalid = true; }
            Check(invalid && h.Locater.Calls.Count == 0 && !h.Has("text.ab"), "Invalid input rejects the whole batch before submission.");
            var missing = "assets/__session_validation/not-in-manifest.txt";
            var absentAsset = "assets/__session_validation/missing.txt";
            var preload = h.Assets.PreloadAssetHandle<TextAsset>(new[] { TextPath, TextPath.ToUpperInvariant(), missing, absentAsset });
            h.Handles.Add(preload);
            await Wait(() => preload.IsDone, "Preload error details");
            await preload;
            Check(preload.State == AssetHandleState.Failed && !preload.Result && preload.TotalCount == 3 && preload.LoadedCount == 1 &&
                preload.FailedCount == 2 && preload.Errors.Count == 2 && ReferenceEquals(preload.Error, preload.Errors[0]) &&
                preload.Errors.Select(error => error.ResourceKey).SequenceEqual(preload.FailedPaths),
                "Failed preload preserves one error per failed path and exposes the first failure as Error.");
            Check(preload.Errors[0].ResourceKey == missing && preload.Errors[0].Exception == null &&
                preload.Errors[1].ResourceKey == absentAsset &&
                preload.Errors[1].Exception != null && h.AssetRefs("text.ab", TextPath) == 1,
                "Mixed batch preserves distinct causes and retains successful resources until Dispose.");
            var firstError = preload.Error;
            preload.Dispose();
            Check(preload.IsDisposed && preload.State == AssetHandleState.Failed && ReferenceEquals(preload.Error, firstError) &&
                preload.Errors.Count == 2 && h.Refs("text.ab") == 0, "Preload release does not erase errors or retain successful leases.");

            using var cts = new CancellationTokenSource();
            var cancelled = h.Assets.PreloadAssetHandle<TextAsset>(new[] { SecondTextPath }, ct: cts.Token);
            h.Handles.Add(cancelled);
            cts.Cancel();
            var didCancel = false;
            try { await cancelled; } catch (OperationCanceledException) { didCancel = true; }
            Check(didCancel && cancelled.State == AssetHandleState.Cancelled && cancelled.Error == null && !cancelled.IsDisposed,
                "Preload cancellation ends waiting without masquerading as explicit disposal or a loading error.");
            await Wait(() => h.AssetRefs("text.ab", SecondTextPath) == 0, "Cancelled preload lease cleanup");
            Check(cancelled.State == AssetHandleState.Cancelled && cancelled.Errors.Count == 0 && cancelled.LoadedCount == 0,
                "Late child completion does not change cancelled preload statistics or errors.");
            cancelled.Dispose();

            using var empty = h.Assets.PreloadAssetHandle<TextAsset>(Array.Empty<string>());
            await empty;
            empty.Dispose();
            Check(empty.State == AssetHandleState.Succeeded && empty.Result && empty.IsDisposed && empty.Error == null,
                "An empty successful preload retains its bool result after Dispose.");
        }

        private static async UniTask SharedCancellationFailureRetry()
        {
            var h = New(true, 1);
            var b = PriorityAsset<TextAsset>(h, TextPath, AssetLoadPriority.High);
            using var cts = new CancellationTokenSource();
            var a = h.Asset<TextAsset>(TextPath, cts.Token);
            cts.Cancel();
            try { await a; } catch (OperationCanceledException) { }
            var original = new InvalidOperationException("Injected shared bundle failure");
            AssetHandle<TextAsset> b2 = null;
            AssetError oldError = null;
            async UniTask RetryB()
            {
                await b;
                oldError = b.Error;
                Check(b.State == AssetHandleState.Failed && oldError.ResourceKey == "text.ab" &&
                    oldError.Message == original.Message && ReferenceEquals(oldError.Exception, original),
                    "Failure publishes the actual failed Bundle and original exception before retry.");
                b.Dispose();
                b2 = PriorityAsset<TextAsset>(h, TextPath, AssetLoadPriority.Low);
                Check(h.AssetRefs("text.ab", TextPath) == 2 && b2.State == AssetHandleState.Pending,
                    "New retry and old cancelled request each retain their own Lease during failure dispatch.");
            }
            var continuation = RetryB();
            h.Provider.Tick(0);
            var oldTask = h.Locater.Manual["text.ab"];
            oldTask.Fail(original);
            h.Provider.Tick(0);
            await continuation;
            Check(a.State == AssetHandleState.Cancelled && a.Error == null && b2.Error == null &&
                h.AssetRefs("text.ab", TextPath) == 1 && h.Refs("text.ab") == 1,
                "Old failure releases only A's Lease and leaves B2's ownership and state intact.");
            Check(await oldTask.Task == null && oldTask.State == TaskState.Failed,
                "Task failure still awaits to null instead of throwing.");
            Check(CachePriority(h, "text.ab") == AssetLoadPriority.Low, "Failed round does not retain its high priority on retry.");
            h.Finish("text.ab", true);
            await Wait(() => b2.IsDone, "Immediate shared retry success");
            Check(b2.State == AssetHandleState.Succeeded && b2.Result != null && b2.Error == null &&
                b.State == AssetHandleState.Failed && b.IsDisposed && ReferenceEquals(b.Error, oldError),
                "Successful retry neither replaces the old error nor revives the failed Handle.");
        }

        private static async UniTask DependencyRetention()
        {
            var h = New();
            var handle = h.Asset<GameObject>(PrefabPath);
            await Wait(() => handle.IsDone, "Dependency retention prefab");
            var prefab = handle.Result;
            var material = prefab.GetComponent<Renderer>().sharedMaterial;
            var peer = h.Assets.InstantiateHandle(PrefabPath, null);
            h.Handles.Add(peer);
            await Wait(() => peer.IsDone, "Second prefab owner");
            handle.Dispose();
            Check(peer.Result.GetComponent<Renderer>().sharedMaterial == material, "Releasing one owner preserves the shared dependency.");
            peer.Dispose();
            Check(h.Refs("a.ab") == 0 && h.Refs("b.ab") == 1,
                "Idle parent starts retention but continues to own its dependency.");
            h.Provider.Tick(299);
            Check(prefab != null && material != null && h.Refs("b.ab") == 1,
                "Retained parent and its material remain intact before parent expiry.");
            var again = h.Asset<GameObject>(PrefabPath);
            Check(again.IsDone && again.Result == prefab && again.Result.GetComponent<Renderer>().sharedMaterial == material,
                "Cached prefab reuses its protected dependency without broken material references.");
            again.Dispose();
            h.Provider.Tick(301);
            Check(!h.Has("a.ab") && h.Has("b.ab") && h.Refs("b.ab") == 0 &&
                h.Cache("b.ab").IsLoaded && h.Cache("b.ab").IsRetained && material != null,
                "Actual parent unload releases dependency and starts its separate retention period.");
            h.Provider.Tick(299);
            Check(h.Has("b.ab") && material != null, "Dependency retains its full 300-second window after parent unload.");
            h.Provider.Tick(2);
            Check(!h.Has("b.ab") && material == null, "Dependency is destroyed after its own retention expires.");
        }

        private static async UniTask DependencyFailure()
        {
            var h = New(true);
            var a = h.Asset<GameObject>(PrefabPath);
            var dependency = h.Asset<Material>(MaterialPath);
            var preload = h.Assets.PreloadAssetHandle<GameObject>(new[] { PrefabPath });
            h.Handles.Add(preload);
            h.Provider.Tick(0);
            var original = new InvalidOperationException("Injected dependency failure");
            h.Locater.Manual["b.ab"].Fail(original);
            h.Provider.Tick(0);
            h.Assets.UnloadUnusedAssets(); h.Provider.Tick(1000);
            Check(!a.IsDone && h.Cache("b.ab").Stage == AssetCacheStage.Idle && h.Refs("b.ab") == 1, "Failed dependency cache stays pinned until parent settles.");
            var main = h.Finish("a.ab", true);
            Check(a.IsDone && a.Result == null && main == null && h.Refs("b.ab") == 0, "Failed group unloads successful main and releases failed dependency.");
            Check(a.State == AssetHandleState.Failed && a.Error.ResourceKey == "b.ab" &&
                a.Error.Message == original.Message && ReferenceEquals(a.Error.Exception, original) &&
                ReferenceEquals(a.Error, dependency.Error),
                "Parent and direct dependency requests receive the same original error identifying the failed Bundle.");
            await preload;
            Check(preload.State == AssetHandleState.Failed && !preload.Result && preload.Errors.Count == 1 &&
                preload.FailedPaths.Single() == PrefabPath && preload.Errors[0].ResourceKey == "b.ab" &&
                ReferenceEquals(preload.Error, a.Error) && ReferenceEquals(preload.Errors[0], a.Error) &&
                h.Refs("a.ab") == 0 && h.Refs("b.ab") == 0,
                "Preload keeps the requested path separate from the failed dependency and preserves error identity after rollback.");
            await UniTask.Yield();

        }

        private static async UniTask ShutdownDuringLocalBundle()
        {
            var h = New();
            var handle = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0f);
            var task = TaskSnapshot().OfType<LoadLocalBundleTask>().Single(t => t.DebugName == "LoadBundle text.ab");
            // Submit synchronously to control shutdown timing; the native request remains asynchronous.
            task.Execute();
            var request = Get<AssetBundleCreateRequest>(task, "_bundleRequest");
            try
            {
                Check(request != null && !task.IsDone, "A real native Bundle request has been submitted before shutdown.");
                await h.Shutdown();
                Check(handle.IsDone && handle.Result == null, "Provider shutdown completes the pending Handle with failure.");
                await WaitWithoutLoaderTick(() => request.isDone, "Canceled native Bundle completion");
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                var bundle = request.assetBundle;
                _case.evidence.Add($"Canceled task={task.State}, request done={request.isDone}, late Bundle alive={bundle != null}.");
                Check(bundle == null, "Shutdown must unload a Bundle that completes after its request has been canceled.");
            }
            finally
            {
                if (request != null && request.isDone && request.assetBundle != null)
                {
                    _case.evidence.Add("Harness-only cleanup of the leaked late local Bundle; the production failure remains recorded.");
                    request.assetBundle.Unload(true);
                }
            }
        }

        private static async UniTask ShutdownBeforeBundleDelivery()
        {
            var h = New();
            var handle = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0f);
            var task = TaskSnapshot().OfType<LoadLocalBundleTask>().Single(t => t.DebugName == "LoadBundle text.ab");
            AssetBundle bundle = null;
            UniTask shutdown = default;
            var waitingForDelivery = false;
            AssetHandle<TextAsset> retry = null;
            task.Task.GetAwaiter().OnCompleted(() =>
            {
                bundle = NativeBundle(task.GetBundle());
                var aliveBeforeClose = bundle != null && ReferenceEquals(task.GetBundle(), task.Result);
                shutdown = h.Shutdown();
                retry = h.Asset<TextAsset>(SecondTextPath);
                waitingForDelivery = aliveBeforeClose && !shutdown.Status.IsCompleted();
            });
            try
            {
                await WaitWithoutLoaderTick(() => task.WaitForCleanupAsync().Status.IsCompleted(), "Local Bundle callback cleanup");
                Check(waitingForDelivery, "Shutdown from the successful Task continuation waits for the pending result callback and cleanup.");
                await shutdown;
                Check(retry != null && retry.State == AssetHandleState.Failed && retry.Result == null, "Closing rejects new work during handoff.");
                _case.evidence.Add($"Task={task.State}, Handle done={handle.IsDone}, completed undelivered Bundle alive={bundle != null}.");
                Check(bundle == null && task.State == TaskState.Succeeded && handle.IsDone && handle.Result == null,
                    "The closed Loader releases the late successful result without handing it to Cache or rewriting Task history.");
            }
            finally
            {
                if (bundle != null)
                {
                    _case.evidence.Add("Harness-only cleanup of the leaked completed undelivered Bundle; the production failure remains recorded.");
                    bundle.Unload(true);
                }
            }
        }

        private static async UniTask ShutdownWithPendingDependency()
        {
            var h = New();
            var group = RuntimeGroup(LiteConst.TaskGroupId.Asset);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value);
            try
            {
                await Wait(() => gates.All(task => task.IsExecuted), "Reserve admission to control native submission");
                var handle = h.Asset<GameObject>(PrefabPath);
                h.Provider.Tick(0);
                var tasks = TaskSnapshot().OfType<LoadLocalBundleTask>().ToArray();
                var mainTask = tasks.Single(task => task.DebugName == "LoadBundle a.ab");
                var dependency = tasks.Single(task => task.DebugName == "LoadBundle b.ab");
                mainTask.Execute();
                await WaitWithoutLoaderTick(() => mainTask.IsDone, "Main Bundle completes first");
                h.Provider.Tick(0);
                var cache = h.Cache("a.ab");
                var main = NativeBundle(cache.Bundle);
                Check(main != null && !cache.IsLoaded && !handle.IsDone, "Cache owns the native main Bundle before dependency delivery.");

                dependency.Execute();
                var request = Get<AssetBundleCreateRequest>(dependency, "_bundleRequest");
                Check(request != null && !request.isDone, "Real dependency load is in flight when shutdown starts.");
                await h.Shutdown();
                Check(main == null && cache.Bundle == null && handle.IsDone && handle.Result == null &&
                    request.isDone && request.assetBundle == null && dependency.State == TaskState.Cancelled && !h.Has("a.ab") && !h.Has("b.ab"),
                    "Shutdown releases the main, drains the late dependency and leaves neither cache alive.");
            }
            finally { await ReleaseGates(gates); }
        }

        private static async UniTask RemoteCleanup()
        {
            using var server = new BundleHttpServer(File.ReadAllBytes(Path.Combine(BundleFolder, "text.ab")));
            var baseline = AssetBundle.GetAllLoadedAssetBundles().Select(bundle => bundle.GetInstanceID()).ToHashSet();
            var info = new BundleInfo(1, "text.ab", new[] { TextPath }, null);
            var pack = new VersionPackInfo("validation", "windows", false, false, new[] { info });
            var locator = new FileRemoteLocator { UriOverride = server.Url };
            var loader = new AssetBundleLoader(locator, pack, 1);
            try
            {
                var callbacks = 0;
                loader.LoadBundle(info, AssetLoadPriority.Normal, _ => callbacks++); loader.Tick(0);
                locator.Task.Execute();
                var requestField = typeof(UnityDownloadBaseTask).GetField("_request", Private);
                var request = (UnityWebRequest)requestField.GetValue(locator.Task);
                Check(request != null && locator.Task.State == TaskState.Running, "A real HTTP Bundle request starts.");
                var handler = (DownloadHandlerAssetBundle)request.downloadHandler;
                Check(!handler.autoLoadAssetBundle && request.disposeDownloadHandlerOnDispose,
                    "The request owns its handler and defers Bundle creation until extraction.");
                await WaitWithoutLoaderTick(() => request.downloadedBytes > 0 || server.Work.IsFaulted, "First HTTP body segment");
                Check(!server.Work.IsFaulted && !handler.isDownloadComplete && locator.Task.State == TaskState.Running,
                    "The controlled HTTP body is incomplete before shutdown.");
                Check(!AssetBundle.GetAllLoadedAssetBundles().Any(bundle => !baseline.Contains(bundle.GetInstanceID())),
                    "Downloading without GetContent has not created a native Bundle.");
                await loader.DisposeAsync(); await loader.DisposeAsync();
                Check(locator.Task.State == TaskState.Cancelled && locator.Task.Result == null && callbacks == 0,
                    "Shutdown cancels without extracting or delivering a Bundle.");
                await WaitWithoutLoaderTick(() => requestField.GetValue(locator.Task) == null, "Cancelled HTTP request disposal");
                server.Dispose();
                await WaitWithoutLoaderTick(() => server.Work.IsCompleted, "Loopback server shutdown");
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                Check(!server.Work.IsFaulted && locator.Task.State == TaskState.Cancelled && locator.Task.GetBundle() == null && callbacks == 0,
                    "Late completion keeps cancellation and does not notify consumers.");
                Check(!AssetBundle.GetAllLoadedAssetBundles().Any(bundle => !baseline.Contains(bundle.GetInstanceID())),
                    "Cancelled HTTP cleanup leaves no new native Bundle.");
            }
            finally
            {
                await loader.DisposeAsync();
                server.Dispose();
            }
        }

        private static async UniTask InstantiateFailureProtocol()
        {
            var h = New();
            var prefab = h.Asset<GameObject>(PrefabPath);
            using var cts = new CancellationTokenSource();
            var cancelled = h.Assets.InstantiateHandle(PrefabPath, null, ct: cts.Token);
            h.Handles.Add(cancelled);
            cts.Cancel();
            var didCancel = false;
            try { await cancelled; } catch (OperationCanceledException) { didCancel = true; }
            Check(didCancel && cancelled.State == AssetHandleState.Cancelled && cancelled.Error == null &&
                !cancelled.IsDisposed, "GameObject token cancellation immediately publishes Cancelled and no error.");
            await Wait(() => prefab.IsDone, "Prefab for instantiate failure");
            Check(cancelled.Result == null && h.AssetRefs("a.ab", PrefabPath, typeof(GameObject)) == 1,
                "Cancelled instantiation releases its late prefab Lease without disturbing the surviving request.");
            var original = new InvalidOperationException("Injected instantiate failure");
            var failed = new GameObjectHandle(prefab, _ => throw original, default);
            h.Handles.Add(failed);
            Exception caught = null;
            try { await failed; } catch (Exception ex) { caught = ex; }
            Check(failed.State == AssetHandleState.Failed && failed.Error.Message == original.Message &&
                failed.Error.ResourceKey == PrefabPath && ReferenceEquals(failed.Error.Exception, original) &&
                ReferenceEquals(caught, original) && prefab.IsDisposed && h.Refs("a.ab") == 0,
                "Instantiation exceptions keep their await behavior and release the prefab owner before observation.");
            failed.Dispose();
            Check(failed.State == AssetHandleState.Failed && failed.IsDisposed && ReferenceEquals(failed.Error.Exception, original),
                "Disposing a failed GameObject Handle preserves its original error.");

            h.Assets.UnloadUnusedAssets();
            var missing = h.Assets.InstantiateHandle("assets/__session_validation/missing.prefab", null);
            h.Handles.Add(missing);
            await missing;
            Check(missing.State == AssetHandleState.Failed && missing.Error.ResourceKey == "assets/__session_validation/missing.prefab" &&
                ReferenceEquals(missing.Error, Get<AssetHandle<GameObject>>(missing, "_prefabHandle").Error),
                "Prefab failure remains a normal await and forwards the same error without wrapping.");

            var success = h.Assets.InstantiateHandle(PrefabPath, null);
            h.Handles.Add(success);
            await Wait(() => success.IsDone, "Successful instance protocol");
            var instance = success.Result;
            success.Dispose();
            await Wait(() => instance == null, "Instance destruction");
            Check(success.State == AssetHandleState.Succeeded && success.IsDisposed && success.Error == null && success.Result == null,
                "Successful GameObject disposal destroys the instance without changing its loading history.");
        }

        private sealed class FileRemoteLocator : IBundleProvider
        {
            internal LoadRemoteBundleTask Task;
            internal string UriOverride;
            public BaseTask LoadVersionPack(string name) => throw new NotSupportedException();
            public ILoadBundleTask LoadBundle(string path, string hash)
            {
                Task = new LoadRemoteBundleTask(UriOverride ?? new Uri(Path.Combine(BundleFolder, path)).AbsoluteUri, null);
                LiteRuntime.Task.AddTask(Task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
                return Task;
            }
        }

        private static async UniTask SharedRetention()
        {
            var h = New();
            var first = h.Asset<TextAsset>(TextPath);
            var second = h.Asset<TextAsset>(TextPath);
            await Wait(() => first.IsDone && second.IsDone, "Shared asset delivery");
            var original = first.Result;
            Check(original != null && second.Result == original && h.Locater.Calls["text.ab"] == 1, "Two owners share one native read.");
            first.Dispose();
            first.Dispose();
            h.Assets.UnloadUnusedAssets();
            Check(second.Result == original, "The remaining Handle protects its resource.");
            second.Dispose();
            var cached = h.Asset<TextAsset>(TextPath);
            Check(cached.IsDone && cached.Result == original, "Retention reuses the original asset.");
            var scale = Time.timeScale;
            try
            {
                LiteRuntime.Setting.Asset.BundleRetainTime = 0.05f;
                cached.Dispose();
                Time.timeScale = 0;
                await Wait(() =>
                {
                    h.Assets.Tick(Time.deltaTime);
                    return !h.Has("text.ab");
                }, "Retention expiry while paused");
            }
            finally { Time.timeScale = scale; }
            Check(original == null, "Expiry destroys the original native object.");
            var fresh = h.Asset<TextAsset>(TextPath);
            await Wait(() => fresh.IsDone, "Same path reload");
            first.Dispose();
            Check(fresh.Result != null && h.Locater.Calls["text.ab"] == 2, "Reload survives old Handle disposal.");
        }

        private static async UniTask SubmittedAsset()
        {
            var h = New(true);
            using var token = new CancellationTokenSource();
            var cancelled = h.Asset<TextAsset>(TextPath, token.Token);
            var other = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0);
            var native = h.Finish("text.ab", true);
            var task = Get<BaseTask>(h.Entry("text.ab", TextPath), "_loadAssetTask");
            task.Execute();
            var request = Get<AssetBundleRequest>(task, "_assetRequest");
            Check(request != null && !request.isDone, "Unity has accepted the shared read.");
            token.Cancel();
            var observed = false;
            try { await cancelled; } catch (OperationCanceledException) { observed = true; }
            Check(observed && native != null && cancelled.Result == null, "Cancellation finishes only this consumer.");
            await Wait(() => other.IsDone, "Remaining asset consumer");
            Check(request.isDone && other.Result != null && cancelled.Result == null, "Only the remaining owner receives the late asset.");
            other.Dispose();
            h.Assets.UnloadUnusedAssets();
            Check(native == null, "The native Bundle is released after the read settles.");
        }
    }
}
