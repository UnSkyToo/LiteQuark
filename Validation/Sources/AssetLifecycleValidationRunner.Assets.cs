using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal static partial class AssetLifecycleValidationRunner
    {
        private static async UniTask UIClosingSkipsUpdate()
        {
            var systemType = Type.GetType("LiteQuark.Runtime.UISystem, LiteQuark.Runtime.Addons", true);
            var system = (ISystem)Activator.CreateInstance(systemType);
            var probeType = CreateUIUpdateProbeType();
            var first = Activator.CreateInstance(probeType);
            var second = Activator.CreateInstance(probeType);
            var state = probeType.GetProperty("State");
            var go = probeType.GetProperty("Go");
            var close = systemType.GetMethods().Single(m => m.Name == "CloseUI" && !m.IsGenericMethod);
            var firstGo = new GameObject("UpdatingUI", typeof(RectTransform), typeof(Canvas));
            var secondGo = new GameObject("ClosingUI", typeof(RectTransform), typeof(Canvas));
            var firstUpdates = 0;
            var secondUpdates = 0;
            var secondCloses = 0;
            try
            {
                probeType.GetField("OnUpdateCallback").SetValue(first, (Action<object>)(_ =>
                {
                    if (++firstUpdates == 1) close.Invoke(system, new[] { second });
                }));
                probeType.GetField("OnUpdateCallback").SetValue(second, (Action<object>)(_ => secondUpdates++));
                probeType.GetField("OnCloseCallback").SetValue(first, (Action<object>)(_ => { }));
                probeType.GetField("OnCloseCallback").SetValue(second, (Action<object>)(_ => secondCloses++));
                probeType.GetMethod("BindGo", new[] { typeof(GameObject) }).Invoke(first, new object[] { firstGo });
                probeType.GetMethod("BindGo", new[] { typeof(GameObject) }).Invoke(second, new object[] { secondGo });
                foreach (var ui in new[] { first, second })
                {
                    state.SetValue(ui, Enum.Parse(state.PropertyType, "Opened"));
                    probeType.GetProperty("System").SetValue(ui, system);
                    // Supply already-opened UI to the existing queue; asset loading is outside this regression.
                    Get<System.Collections.IList>(system, "_openList").Add(ui);
                }

                ((ITick)system).Tick(0.02f);
                Check(firstUpdates == 1 && secondCloses == 1 && secondUpdates == 0 && state.GetValue(second).ToString() == "Closing",
                    "A closes B during Update: B receives OnClose immediately and no OnUpdate later in the same Tick.");
                Check((GameObject)go.GetValue(second) == secondGo && secondGo != null,
                    "Closing keeps the instance alive until the existing close queue is processed next Tick.");
                ((ITick)system).Tick(0.02f);
                Check(firstUpdates == 2 && secondUpdates == 0 && secondCloses == 1 && state.GetValue(second).ToString() == "Closed" &&
                    (GameObject)go.GetValue(second) == null,
                    "Next Tick releases B through the close queue, without repeating OnClose, while A continues updating.");
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Check(secondGo == null, "The closed UI's GameObject is actually destroyed after deferred Unity destruction.");
            }
            finally
            {
                await system.DisposeAsync();
                if (firstGo != null) UnityEngine.Object.DestroyImmediate(firstGo);
                if (secondGo != null) UnityEngine.Object.DestroyImmediate(secondGo);
            }
        }

        private static async UniTask PoolTemplateCompletion()
        {
            var template = new GameObject("PoolTemplateCompletion");
            try
            {
                foreach (var outcome in new[] { "success", "failure", "cancel" })
                {
                    var pools = new ObjectPoolSystem();
                    await pools.Initialize();
                    var pool = pools.AddPool<DeferredTemplatePool>(outcome, pools.GetPoolRoot());
                    var ready = pool.WaitReadyAsync();
                    var allocation = pool.Alloc();
                    var preload = pools.PreloadPoolAsync<DeferredTemplatePool>(outcome, 2);
                    var callbackCount = 0;
                    GameObject callbackObject = null;
                    pool.Alloc(go => { callbackCount++; callbackObject = go; });
                    var generated = pool.GenerateAsync(1);
                    try
                    {
                        Check(ready.Status == UniTaskStatus.Pending && allocation.Status == UniTaskStatus.Pending,
                            "Requests wait while the template has no result.");
                        if (outcome == "cancel")
                        {
                            await pools.DisposeAsync();
                            await CheckPoolCancelled(ready);
                            await CheckPoolCancelled(allocation);
                            await CheckPoolCancelled(preload);
                            await CheckPoolCancelled(generated);
                            pool.Finish(template);
                            Check(!pool.IsReady && callbackCount == 1 && callbackObject == null,
                                "Closing cancels all waits, sends null callbacks once and rejects a late template.");
                            var rejected = false;
                            try { pools.GetPool<DeferredTemplatePool>("new"); }
                            catch (ObjectDisposedException) { rejected = true; }
                            Check(rejected && pools.GetPoolCache().Count == 0,
                                "Closing cannot recreate a pool through a resumed preload or a new lookup.");
                        }
                        else
                        {
                            pool.Finish(outcome == "success" ? template : null);
                            if (outcome == "failure")
                            {
                                Check(!await ready && await allocation == null && !await preload && !await generated &&
                                    callbackCount == 1 && callbackObject == null,
                                    "A failed template ends both callback and await requests without creating objects.");
                                Check(!await pool.WaitReadyAsync() && await pool.Alloc() == null && !await pool.GenerateAsync(1),
                                    "Later requests observe the existing failure rather than waiting forever.");
                            }
                            else
                            {
                                await Wait(() => preload.Status.IsCompleted() && generated.Status.IsCompleted(), "Pool generation after template completion");
                                var allocated = await allocation;
                                Check(await ready && await preload && await generated && callbackCount == 1 && callbackObject != null &&
                                    allocated != null,
                                    "Successful template completion reaches every consumer and completes native pre-generation.");
                                Check(pool.CountActive == 2 && pool.CountInactive == 3 && pool.CountAll == 5,
                                    "Generated objects do not corrupt borrowed and inactive counts.");
                                pools.RemoveUnusedPools();
                                Check(pools.GetPoolCache().Count == 1, "An active pool survives unused-pool collection after pre-generation.");
                                pool.Recycle(allocated);
                                pool.Recycle(callbackObject);
                            }
                        }
                    }
                    finally
                    {
                        if (pools.GetPoolRoot() != null) await pools.DisposeAsync();
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(template); }

            foreach (var removeUnused in new[] { false, true })
            {
                var pools = new ObjectPoolSystem();
                await pools.Initialize();
                var root = pools.GetPoolRoot();
                var oldPool = pools.AddPool<DeferredTemplatePool>("PoolReplacement", root);
                DeferredTemplatePool replacement = null;
                var callbacks = 0;
                var receivedNull = false;
                oldPool.Alloc(go =>
                {
                    callbacks++;
                    receivedNull = go == null;
                    pools.RemovePool(oldPool);
                    replacement = pools.GetPool<DeferredTemplatePool>(oldPool.Key, root);
                });
                try
                {
                    if (removeUnused) pools.RemoveUnusedPools();
                    else pools.RemovePool(oldPool);
                    Check(callbacks == 1 && receivedNull && replacement != null && !ReferenceEquals(oldPool, replacement),
                        "Removing a pending pool delivers cancellation once and allows its callback to create a replacement.");
                    Check(ReferenceEquals(pools.GetPool<DeferredTemplatePool>(oldPool.Key), replacement),
                        "Both direct and unused-pool removal preserve the new registration after the cancellation callback returns.");
                    var ready = replacement.WaitReadyAsync();
                    pools.RemovePool(oldPool);
                    Check(ready.Status == UniTaskStatus.Pending && ReferenceEquals(pools.GetPool<DeferredTemplatePool>(oldPool.Key), replacement),
                        "Removing an old instance cannot dispose or unregister its same-key replacement.");
                    await pools.DisposeAsync();
                    await CheckPoolCancelled(ready);
                    await UniTask.NextFrame();
                    Check(root == null, "System shutdown still owns and cleans the replacement pool's hierarchy.");
                }
                finally
                {
                    if (pools.GetPoolRoot() != null) await pools.DisposeAsync();
                }
            }
            await PoolBatchRemovalCallbacks();
        }

        private static async UniTask PoolBatchRemovalCallbacks()
        {
            var pools = new ObjectPoolSystem();
            await pools.Initialize();
            var root = pools.GetPoolRoot();
            var first = pools.AddPool<DeferredTemplatePool>("ClosingFirst", root);
            var second = pools.AddPool<DeferredTemplatePool>("ClosingSecond", root);
            var callbacks = 0;
            var allCancelled = true;
            first.Alloc(go => { callbacks++; allCancelled &= go == null; pools.RemovePool(first); });
            second.Alloc(go => { callbacks++; allCancelled &= go == null; pools.RemovePool(second); });
            try
            {
                await pools.DisposeAsync();
                Check(callbacks == 2 && allCancelled && !first.IsReady && !second.IsReady,
                    "Shutdown completes every pending pool even when cancellation callbacks remove their pools.");
                await UniTask.NextFrame();
                Check(root == null, "Cancellation callbacks cannot interrupt shutdown before the root is destroyed.");
            }
            finally
            {
                if (pools.GetPoolRoot() != null) await pools.DisposeAsync();
            }

            pools = new ObjectPoolSystem();
            await pools.Initialize();
            var pending = pools.AddPool<DeferredTemplatePool>("UnusedPending", pools.GetPoolRoot());
            var ready = pools.GetEmptyGameObjectPool("UnusedReady");
            GameObject borrowed = null;
            pending.Alloc(go =>
            {
                if (go == null) ready.Alloc(value => borrowed = value);
            });
            try
            {
                pools.RemoveUnusedPools();
                Check(borrowed != null && ready.CountActive == 1 && ReferenceEquals(pools.GetEmptyGameObjectPool(ready.Key), ready),
                    "Unused-pool collection keeps a candidate borrowed from an earlier cancellation callback.");
                await UniTask.NextFrame();
                Check(borrowed != null && ready.IsReady,
                    "The newly borrowed object survives the candidate-removal frame under its original pool parent.");
                ready.Recycle(borrowed);
                pools.RemoveUnusedPools();
                await UniTask.NextFrame();
                Check(borrowed == null && pools.GetPoolCache().Count == 0,
                    "The preserved pool can still be collected after its borrowed instance is returned.");
            }
            finally { await pools.DisposeAsync(); }
        }

        private static async UniTask CheckPoolCancelled<T>(UniTask<T> request)
        {
            var cancelled = false;
            try { await request; }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled, "An asynchronous pool request preserves cancellation instead of returning ordinary failure.");
        }

        private static async UniTask PoolGenerationShutdown()
        {
            foreach (var running in new[] { false, true })
            {
                var pools = new ObjectPoolSystem();
                await pools.Initialize();
                var root = pools.GetPoolRoot();
                var pool = pools.GetEmptyGameObjectPool("PoolGenerationShutdown");
                var outside = new GameObject("PoolBorrower");
                GameObject borrowed = null;
                try
                {
                    borrowed = await pool.Alloc(outside.transform);
                    var generated = pool.GenerateAsync(8);
                    var secondGenerated = pool.GenerateAsync(8);
                    if (running) LiteRuntime.Task.Tick(0);
                    var shutdown = pools.DisposeAsync();
                    Check(shutdown.Status.IsCompleted(), "Pool shutdown remains synchronous even with pre-generation work.");
                    await shutdown;
                    Check(generated.Status == UniTaskStatus.Canceled && secondGenerated.Status == UniTaskStatus.Canceled && !pool.IsReady,
                        "All queued and running generation requests are canceled when synchronous pool shutdown returns.");
                    await CheckPoolCancelled(generated);
                    await CheckPoolCancelled(secondGenerated);
                    await UniTask.NextFrame();
                    await UniTask.NextFrame();
                    Check(root == null && borrowed != null && pool.CountAll == 0,
                        "Pool-owned nodes disappear with no late success; externally parented borrowed instances remain the owner's responsibility.");
                }
                finally
                {
                    if (pools.GetPoolRoot() != null) await pools.DisposeAsync();
                    UnityEngine.Object.DestroyImmediate(outside);
                }
            }

            var releaseFailureSystem = new ObjectPoolSystem();
            await releaseFailureSystem.Initialize();
            try
            {
                var pool = releaseFailureSystem.AddPool<ThrowingReleasePool>("PoolReleaseFailure", releaseFailureSystem.GetPoolRoot());
                var generated = pool.GenerateAsync(3);
                await Wait(() => generated.Status.IsCompleted(), "Pool release failure");
                Exception failure = null;
                try { await generated; }
                catch (Exception ex) { failure = ex; }
                Check(ReferenceEquals(failure, pool.Error), "An overridden release failure propagates through GenerateAsync instead of returning false.");
                await UniTask.NextFrame();
                Check(pool.CountInactive == 1 && releaseFailureSystem.GetPoolRoot().GetChild(0).childCount == 2,
                    "Partial generation retains the accepted object and template, but destroys all unaccepted instances.");
                var next = pool.GenerateAsync(1);
                await Wait(() => next.Status.IsCompleted(), "Generation after a release failure");
                Check(await next && pool.CountInactive == 2, "A finished failed batch does not block a later generation request.");
            }
            finally { await releaseFailureSystem.DisposeAsync(); }

            var partial = new ActiveGameObjectPool();
            partial.Dispose();
            var failedPoolSystem = new ObjectPoolSystem();
            await failedPoolSystem.Initialize();
            var failedRoot = failedPoolSystem.GetPoolRoot();
            try
            {
                var threw = false;
                try { failedPoolSystem.GetActiveGameObjectPool("../invalid.prefab"); }
                catch (ArgumentException) { threw = true; }
                await Wait(() => failedRoot.childCount == 0, "Failed pool initialization node cleanup");
                Check(threw && failedPoolSystem.GetPoolCache().Count == 0 && failedRoot.childCount == 0,
                    "A pool whose initialization throws releases its node without entering the cache.");
            }
            finally { await failedPoolSystem.DisposeAsync(); }
        }

        private static async UniTask ActivePoolShutdown()
        {
            var h = New();
            var assets = LiteRuntime.Asset;
            var original = Get<IAssetProvider>(assets, "_provider");
            Set(assets, "_provider", h.Provider);
            var pools = new ObjectPoolSystem();
            await pools.Initialize();
            try
            {
                var warmup = pools.PreloadPoolAsync<ActiveGameObjectPool>(PrefabPath, 2, pools.GetPoolRoot());
                await Wait(() => warmup.Status.IsCompleted(), "Active pool native warmup");
                Check(await warmup, "Active pool preloads a real Bundle prefab and completes generated instances.");
                var pool = pools.GetActiveGameObjectPool(PrefabPath);
                var go = await pool.Alloc();
                Check(go != null && go.activeSelf && pool.CountInactive == 1, "Async allocation preserves Active pool activation.");
                pool.Recycle(go);
                Check(!go.activeSelf, "Recycling preserves Active pool deactivation.");

                var failed = pools.GetActiveGameObjectPool("assets/__session_validation/missing.txt");
                var failedReady = failed.WaitReadyAsync();
                await Wait(() => failedReady.Status.IsCompleted(), "Failed pool template");
                Check(!await failedReady && await failed.Alloc() == null,
                    "A real template failure is returned as false/null rather than leaving consumers pending.");

                var pending = pools.GetActiveGameObjectPool("assets/__session_validation/c.prefab");
                var allocation = pending.Alloc();
                var preload = pools.PreloadPoolAsync<ActiveGameObjectPool>(pending.Key, 3);
                await pools.DisposeAsync();
                await CheckPoolCancelled(allocation);
                await CheckPoolCancelled(preload);
                await h.Shutdown();
                Check(!pending.IsReady && go == null && !h.Has("a.ab") && !h.Has("b.ab"),
                    "Pool close releases templates before Asset shutdown drains late loads and unloads shared dependencies.");
            }
            finally
            {
                if (pools.GetPoolRoot() != null) await pools.DisposeAsync();
                Set(assets, "_provider", original);
            }
        }

        private static async UniTask ShutdownWaitsForRetiredBundle()
        {
            var h = New(concurrency: 1);
            var handle = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0);
            var task = TaskSnapshot().OfType<LoadLocalBundleTask>().Single(t => t.DebugName == "LoadBundle text.ab");
            task.Execute();
            var request = Get<AssetBundleCreateRequest>(task, "_bundleRequest");
            task.Fail(new InvalidOperationException("Controlled failure before native Bundle completion"));
            task.Dispose();
            h.Provider.Tick(0);
            Check(handle.State == AssetHandleState.Failed && task.WaitForCleanupAsync().Status == UniTaskStatus.Pending,
                "The logical request has failed while its native Bundle cleanup remains pending.");
            var other = h.Asset<GameObject>(PrefabPath);
            h.Provider.Tick(0);
            Check(h.Locater.Started.Count == 2 && h.Locater.Started[1] != "text.ab" && !other.IsDone,
                "Another Bundle request is submitted despite the earlier task's pending native cleanup.");
            var shutdown = h.Shutdown();
            Check(shutdown.Status == UniTaskStatus.Pending, "System shutdown includes the previously retired resource task.");
            await shutdown;
            Check(request.isDone && request.assetBundle == null && task.WaitForCleanupAsync().Status == UniTaskStatus.Succeeded && other.IsDone,
                "System close waits for the earlier native cleanup and ends the other request without leaking its late Bundle.");
        }

        private static async UniTask ShutdownWaitsAfterCleanupFailure()
        {
            var h = New();
            var handle = h.Asset<TextAsset>(TextPath);
            var other = h.Asset<TextAsset>(SecondTextPath);
            await Wait(() => handle.IsDone && other.IsDone, "Assets before failing cleanup");
            var bundle = h.Cache("text.ab").Bundle;
            var first = new CleanupProbe();
            var second = new CleanupProbe();
            var error = new InvalidOperationException("Controlled resource cleanup failure");
            var firstEntry = h.Entry("text.ab", TextPath);
            var secondEntry = h.Entry("text.ab", SecondTextPath);
            // Each cache owns its cleanup; use controlled tails to keep one pending after another faults.
            Set(firstEntry, "_loadAssetTask", first);
            Set(secondEntry, "_loadAssetTask", second);
            first.FinishCleanup(error);
            var shutdown = h.Shutdown();
            await Frame(); await Frame();
            Check(shutdown.Status == UniTaskStatus.Pending && second.State == TaskState.Cancelled && bundle != null,
                "A peer cleanup fault cannot bypass pending cleanup or trigger Bundle unloading.");
            second.FinishCleanup();
            Exception failure = null;
            try { await shutdown; } catch (Exception ex) { failure = ex; }
            Check(ReferenceEquals(failure, error) && bundle != null && !h.Disposed,
                "After all cleanup settles, close reports the original failure without unloading the Bundle or reporting success.");
            var rejected = h.Asset<TextAsset>(SecondTextPath);
            Check(rejected.State == AssetHandleState.Failed, "A failed close stays closed to new requests.");
        }

        private static async UniTask DownloadReleaseFailures()
        {
            New();
            var timerField = typeof(UnityDownloadBaseTask).GetField("_retryTimerID", Private);
            var requestField = typeof(UnityDownloadBaseTask).GetField("_request", Private);
            foreach (var mode in new[] { "retry", "success", "cancel", "fail" })
            {
                using var server = new BundleHttpServer(new byte[16]);
                var releaseError = new InvalidOperationException("Controlled request release failure: " + mode);
                var logicalError = new InvalidOperationException("Controlled logical download failure");
                var task = new RequestReleaseProbe(server.Url, releaseError, mode == "retry");
                async UniTask<Exception> ObserveFailure()
                {
                    try { await task.WaitForCleanupAsync(); return null; }
                    catch (Exception ex) { return ex; }
                }
                try
                {
                    var first = ObserveFailure();
                    var second = ObserveFailure();
                    if (mode == "success") task.Task.GetAwaiter().OnCompleted(task.Dispose);
                    task.Execute();
                    if (mode != "retry")
                    {
                        await WaitWithoutLoaderTick(() => task.Requests[0].downloadedBytes > 0 || server.Work.IsFaulted,
                            "Request release fixture receives HTTP data");
                        Check(!server.Work.IsFaulted && !task.IsDone, "Terminal release failure targets a running request.");
                        // Set each logical terminal state deliberately; normal download success/handoff is covered by RemoteTaskCleanup.
                        if (mode == "success") Check(task.CompleteSuccessfully(), "The fixture publishes success before injecting a release failure.");
                        else if (mode == "cancel") task.Cancel();
                        else task.Fail(logicalError);
                    }
                    await WaitWithoutLoaderTick(() => first.Status.IsCompleted() && second.Status.IsCompleted(),
                        "Request release failure reaches both cleanup observers");
                    Check(ReferenceEquals(await first, releaseError) && ReferenceEquals(await second, releaseError) &&
                        task.Handler.Throws == 1 && task.CleanupReports == 1,
                        mode + ": release failure reaches both observers once without escaping the native callback.");
                    if (mode == "retry")
                    {
                        Check(task.RetryTimerAtRelease != null && task.RetryTimerAtRelease.IsDone &&
                            (ulong)timerField.GetValue(task) == 0 && task.State == TaskState.Failed && ReferenceEquals(task.Error, releaseError),
                            "The failed request had scheduled a retry; its release failure terminates the task and cancels that timer.");
                        task.RetryTimerAtRelease.Tick(31f);
                    }
                    var expectedState = mode == "success" ? TaskState.Succeeded : mode == "cancel" ? TaskState.Cancelled : TaskState.Failed;
                    var expectedError = mode == "retry" ? releaseError : mode == "fail" ? logicalError : null;
                    var result = await task.Task;
                    Check(task.State == expectedState && ReferenceEquals(task.Error, expectedError) &&
                        (mode == "success" ? result is true : result == null) &&
                        task.Failures == (expectedState == TaskState.Failed ? 1 : 0),
                        mode + ": cleanup failure preserves any established logical result and only terminates an unfinished retry.");
                    task.Dispose(); task.Dispose(); task.Cancel(); task.Fail(logicalError);
                    Check(ReferenceEquals(await ObserveFailure(), releaseError) && task.CleanupReports == 1 &&
                        task.Requests.Count == 1 && requestField.GetValue(task) == null && (ulong)timerField.GetValue(task) == 0,
                        "Later retirement and repeated observation cannot restart downloading or replace the original cleanup failure.");
                }
                finally
                {
                    task.Cancel(); task.Dispose();
                    // The injected handler error interrupts UnityWebRequest.Dispose; fixture isolation releases the retained native request.
                    foreach (var request in task.Requests) request.Dispose();
                    server.Dispose();
                    await WaitWithoutLoaderTick(() => server.Work.IsCompleted, "Request release fixture socket cleanup");
                    Check(!server.Work.IsFaulted, "Request release fixture closes its socket worker without an unhandled error.");
                }
            }
        }

        private static async UniTask PendingResourceTaskCleanup()
        {
            foreach (var fail in new[] { false, true })
            {
                var callbacks = new int[4];
                var tasks = new BaseTask[]
                {
                    new LoadLocalBundleTask("unused.ab", _ => callbacks[0]++),
                    new LoadAssetTask<TextAsset>(null, TextPath, _ => callbacks[1]++),
                    new LoadSceneTask(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive), _ => callbacks[2]++),
                    new LoadRemoteBundleTask(new Uri(Path.Combine(BundleFolder, "text.ab")).AbsoluteUri, null, _ => callbacks[3]++)
                };
                try
                {
                    for (var i = 0; i < tasks.Length; i++)
                    {
                        var task = tasks[i];
                        var first = ObserveResourceCleanup(task);
                        var second = ObserveResourceCleanup(task);
                        Check(first.Status == UniTaskStatus.Pending && second.Status == UniTaskStatus.Pending &&
                            task.State == TaskState.Pending && !task.IsExecuted && callbacks[i] == 0,
                            task.DebugName + " repeated Pending cleanup observation does not execute or notify.");
                        var error = new InvalidOperationException("Controlled pending resource Task failure");
                        if (fail) task.Fail(error);
                        else task.Cancel();
                        Check(await task.Task == null && task.State == (fail ? TaskState.Failed : TaskState.Cancelled) &&
                            task.Result == null && ReferenceEquals(task.Error, fail ? error : null) && !task.IsExecuted,
                            task.DebugName + " logical failure/cancellation awaits normally to null without execution.");
                        Check(first.Status == UniTaskStatus.Succeeded && second.Status == UniTaskStatus.Succeeded,
                            task.DebugName + " no-native-operation cleanup completes at termination.");
                        task.Dispose(); task.Dispose(); task.Cancel(); task.Fail(error);
                        await WaitForResourceCleanup(first, second, "Pending resource cleanup");
                        await ObserveResourceCleanup(task);
                        var expectedCallbacks = fail || i == 1 || i == 2 ? 1 : 0;
                        Check(callbacks[i] == expectedCallbacks && task.State == (fail ? TaskState.Failed : TaskState.Cancelled) &&
                            task.Result == null && ReferenceEquals(task.Error, fail ? error : null) && !task.IsExecuted,
                            task.DebugName + " repeated cleanup preserves the original terminal callback count, error and null result.");
                    }
                }
                finally
                {
                    foreach (var task in tasks) { task.Cancel(); task.Dispose(); }
                }
            }
        }

        private static async UniTask RetireLocalBundleFixture(LoadLocalBundleTask task, AssetBundleCreateRequest request)
        {
            task.Cancel(); task.Dispose();
            if (request == null) return;
            await WaitWithoutLoaderTick(() => request.isDone, "Local Bundle fixture native completion");
            if (request.assetBundle != null) request.assetBundle.Unload(true);
        }

        private static async UniTask LocalBundleTaskCleanup()
        {
            foreach (var fail in new[] { false, true })
            {
                var callbacks = 0;
                var task = new LoadLocalBundleTask(Path.Combine(BundleFolder, "text.ab"), _ => callbacks++);
                AssetBundleCreateRequest request = null;
                try
                {
                    var first = ObserveResourceCleanup(task);
                    task.Execute();
                    request = Get<AssetBundleCreateRequest>(task, "_bundleRequest");
                    var second = ObserveResourceCleanup(task);
                    Check(request != null && !request.isDone && first.Status == UniTaskStatus.Pending && second.Status == UniTaskStatus.Pending,
                        "Local Bundle cleanup observes a real outstanding native request.");
                    var error = new InvalidOperationException("Controlled started local Bundle failure");
                    if (fail) task.Fail(error);
                    else task.Cancel();
                    task.Dispose(); task.Dispose();
                    Check(await task.Task == null && first.Status == UniTaskStatus.Pending && second.Status == UniTaskStatus.Pending,
                        "Local Bundle logical await ends before native cleanup, even after wrapper retirement.");
                    await WaitForResourceCleanup(first, second, "Retired local Bundle cleanup");
                    await ObserveResourceCleanup(task);
                    task.Cancel(); task.Fail(new Exception("Must not replace local terminal state"));
                    Check(request.isDone && request.assetBundle == null && callbacks == (fail ? 1 : 0) &&
                        task.State == (fail ? TaskState.Failed : TaskState.Cancelled) && task.Result == null &&
                        ReferenceEquals(task.Error, fail ? error : null) && await task.Task == null,
                        "Both cleanup waiters finish only after late Bundle unloading, preserving the original logical outcome and callback count.");
                }
                finally
                {
                    await RetireLocalBundleFixture(task, request);
                }
            }

            var events = new List<string>();
            AssetBundle delivered = null;
            var calls = 0;
            var success = new LoadLocalBundleTask(Path.Combine(BundleFolder, "text.ab"), bundle =>
            {
                delivered = bundle; calls++; events.Add("callback");
            });
            AssetBundleCreateRequest successRequest = null;
            try
            {
                var continuation = DisposeInResourceTaskAwait(success, events);
                var first = ObserveResourceCleanup(success);
                var second = ObserveResourceCleanup(success);
                success.Execute();
                successRequest = Get<AssetBundleCreateRequest>(success, "_bundleRequest");
                await WaitForResourceCleanup(first, second, "Successful local Bundle cleanup");
                var awaited = await continuation;
                await ObserveResourceCleanup(success);
                Check(success.State == TaskState.Succeeded && success.Error == null && delivered != null && calls == 1 &&
                    ReferenceEquals(awaited, delivered) && ReferenceEquals(success.GetBundle(), delivered) &&
                    events.SequenceEqual(new[] { "await", "callback" }),
                    "Immediate wrapper Dispose in the local Bundle await continuation preserves the one-shot success handoff and does not unload the Bundle.");
            }
            finally
            {
                await RetireLocalBundleFixture(success, successRequest);
            }
        }

        private static async UniTask AssetTaskCleanup()
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(BundleFolder, "text.ab"));
            LoadAssetTask<TextAsset> cancelled = null;
            LoadAssetTask<TextAsset> success = null;
            AssetBundleRequest cancelledRequest = null;
            AssetBundleRequest successRequest = null;
            try
            {
                Check(bundle != null, "Asset cleanup uses an independently owned real fixture Bundle.");
                var cancelledCalls = 0;
                cancelled = new LoadAssetTask<TextAsset>(bundle, AssetPath.Resolve(TextPath), _ => cancelledCalls++);
                var first = ObserveResourceCleanup(cancelled);
                cancelled.Execute();
                cancelledRequest = Get<AssetBundleRequest>(cancelled, "_assetRequest");
                var second = ObserveResourceCleanup(cancelled);
                Check(cancelledRequest != null && !cancelledRequest.isDone, "Asset cancellation targets an outstanding native read.");
                cancelled.Cancel(); cancelled.Dispose(); cancelled.Dispose();
                Check(await cancelled.Task == null && first.Status == UniTaskStatus.Pending && second.Status == UniTaskStatus.Pending && cancelledCalls == 0,
                    "Submitted Asset cancellation ends logical waiting without delivering an object or prematurely completing cleanup.");
                await WaitForResourceCleanup(first, second, "Retired Asset native cleanup");
                await ObserveResourceCleanup(cancelled);
                var retained = bundle.LoadAsset<TextAsset>(AssetPath.Resolve(TextPath));
                Check(cancelledRequest.isDone && cancelled.State == TaskState.Cancelled && cancelled.Error == null &&
                    cancelled.Result == null && cancelled.GetAsset() == null && cancelledCalls == 0 && bundle != null && retained != null,
                    "Late Asset completion stays undispatched after wrapper disposal; cleanup leaves the upper owner's Bundle and asset usable.");

                var events = new List<string>();
                TextAsset delivered = null;
                var calls = 0;
                success = new LoadAssetTask<TextAsset>(bundle, AssetPath.Resolve(SecondTextPath), completed =>
                {
                    delivered = completed.Result as TextAsset; calls++; events.Add("callback");
                });
                var continuation = DisposeInResourceTaskAwait(success, events);
                var successFirst = ObserveResourceCleanup(success);
                var successSecond = ObserveResourceCleanup(success);
                success.Execute();
                successRequest = Get<AssetBundleRequest>(success, "_assetRequest");
                await WaitForResourceCleanup(successFirst, successSecond, "Successful Asset cleanup");
                var awaited = await continuation;
                await ObserveResourceCleanup(success);
                Check(success.State == TaskState.Succeeded && success.Error == null && delivered != null && calls == 1 &&
                    ReferenceEquals(awaited, delivered) && ReferenceEquals(success.GetAsset(), delivered) && retained != null && bundle != null &&
                    events.SequenceEqual(new[] { "await", "callback" }),
                    "Immediate wrapper Dispose in the Asset await continuation still hands off the same asset once and unloads neither asset nor Bundle.");
            }
            finally
            {
                cancelled?.Cancel(); cancelled?.Dispose(); success?.Cancel(); success?.Dispose();
                if (cancelledRequest != null) await WaitWithoutLoaderTick(() => cancelledRequest.isDone, "Cancelled Asset fixture native completion");
                if (successRequest != null) await WaitWithoutLoaderTick(() => successRequest.isDone, "Successful Asset fixture native completion");
                if (bundle != null) bundle.Unload(true);
            }
        }

        private static T GetDownloadField<T>(LoadRemoteBundleTask task, string field) =>
            (T)typeof(UnityDownloadBaseTask).GetField(field, Private).GetValue(task);

        private static async UniTask RetireRemoteFixture(LoadRemoteBundleTask task)
        {
            task.Cancel(); task.Dispose();
            await WaitWithoutLoaderTick(() => GetDownloadField<UnityWebRequest>(task, "_request") == null &&
                GetDownloadField<ulong>(task, "_retryTimerID") == 0 && !TaskSnapshot().Contains(task), "Remote fixture retirement");
            var bundle = task.GetBundle();
            if (bundle != null) bundle.Unload(true);
        }

        private static async UniTask RemoteTaskCleanup()
        {
            var events = new List<string>();
            var calls = 0;
            AssetBundle delivered = null;
            var success = new LoadRemoteBundleTask(new Uri(Path.Combine(BundleFolder, "text.ab")).AbsoluteUri, null, bundle =>
            {
                delivered = bundle; calls++; events.Add("callback");
            });
            try
            {
                var continuation = DisposeInResourceTaskAwait(success, events);
                var first = ObserveResourceCleanup(success);
                var second = ObserveResourceCleanup(success);
                var deliveredAtCleanup = false;
                async UniTask ObserveHandoff()
                {
                    await first;
                    events.Add("cleanup");
                    deliveredAtCleanup = calls == 1 && delivered != null &&
                        GetDownloadField<UnityWebRequest>(success, "_request") == null;
                }
                var handoff = ObserveHandoff();
                LiteRuntime.Task.AddTask(success, LiteConst.TaskGroupId.Asset);
                await WaitForResourceCleanup(first, second, "Successful remote-file cleanup");
                await handoff;
                Check(deliveredAtCleanup && events.SequenceEqual(new[] { "await", "callback", "cleanup" }),
                    "The cleanup continuation resumes only after request disposal and the success notification, without waiting for record retirement.");
                var awaited = await continuation;
                await WaitWithoutLoaderTick(() => !TaskSnapshot().Contains(success), "Successful remote wrapper retirement");
                await ObserveResourceCleanup(success);
                Check(success.State == TaskState.Succeeded && success.Error == null && delivered != null && calls == 1 &&
                    ReferenceEquals(awaited, delivered) && ReferenceEquals(success.GetBundle(), delivered) &&
                    GetDownloadField<UnityWebRequest>(success, "_request") == null && GetDownloadField<ulong>(success, "_retryTimerID") == 0 &&
                    events.SequenceEqual(new[] { "await", "callback", "cleanup" }),
                    "Remote-file success survives await-Dispose handoff once; repeated cleanup waits finish after request/timer release without unloading the delivered Bundle.");
            }
            finally
            {
                await RetireRemoteFixture(success);
            }

            var body = File.ReadAllBytes(Path.Combine(BundleFolder, "text.ab"));
            using (var server = new BundleHttpServer(body))
            {
                var baseline = AssetBundle.GetAllLoadedAssetBundles().Select(bundle => bundle.GetInstanceID()).ToHashSet();
                var cancelledCalls = 0;
                var cancelled = new LoadRemoteBundleTask(server.Url, null, _ => cancelledCalls++);
                try
                {
                    var first = ObserveResourceCleanup(cancelled);
                    var second = ObserveResourceCleanup(cancelled);
                    LiteRuntime.Task.AddTask(cancelled, LiteConst.TaskGroupId.Asset);
                    cancelled.Execute();
                    var request = GetDownloadField<UnityWebRequest>(cancelled, "_request");
                    Check(request != null && cancelled.State == TaskState.Running, "Remote cancellation starts a real loopback HTTP transfer.");
                    await WaitWithoutLoaderTick(() => request.downloadedBytes > 0 || server.Work.IsFaulted, "Remote cleanup first HTTP segment");
                    Check(!server.Work.IsFaulted && !((DownloadHandlerAssetBundle)request.downloadHandler).isDownloadComplete,
                        "Loopback HTTP cancellation occurs with its body held mid-transfer.");
                    cancelled.Cancel(); cancelled.Cancel();
                    Check(await cancelled.Task == null && cancelled.State == TaskState.Cancelled && cancelled.Error == null && cancelled.Result == null &&
                        GetDownloadField<UnityWebRequest>(cancelled, "_request") == null,
                        "Cancellation ends logical waiting and releases its request without waiting for scheduler retirement.");
                    await WaitForResourceCleanup(first, second, "Cancelled HTTP cleanup without retirement");
                    Check(TaskSnapshot().Contains(cancelled) && cancelledCalls == 0,
                        "Cleanup completes while the scheduler still holds the task record.");
                    await WaitWithoutLoaderTick(() => !TaskSnapshot().Contains(cancelled), "Cancelled HTTP wrapper retirement");
                    await ObserveResourceCleanup(cancelled);
                    Check(GetDownloadField<UnityWebRequest>(cancelled, "_request") == null && GetDownloadField<ulong>(cancelled, "_retryTimerID") == 0 &&
                        cancelled.State == TaskState.Cancelled && cancelled.Result == null && cancelled.Error == null && cancelledCalls == 0 &&
                        !AssetBundle.GetAllLoadedAssetBundles().Any(bundle => !baseline.Contains(bundle.GetInstanceID())),
                        "Multiple HTTP cleanup waiters observe request/timer release and no late callback or extracted Bundle.");
                }
                finally
                {
                    try { await RetireRemoteFixture(cancelled); }
                    finally
                    {
                        server.Dispose();
                        await WaitWithoutLoaderTick(() => server.Work.IsCompleted, "Cancelled HTTP fixture socket cleanup");
                        Check(!server.Work.IsFaulted, "Cancelled HTTP fixture closes its socket worker without an unhandled error.");
                    }
                }
            }

            var previousRetry = LiteRuntime.Setting.Asset.BundleDownloadRetry;
            try
            {
                LiteRuntime.Setting.Asset.BundleDownloadRetry = new RetryParam(1, 1, 30f);
                foreach (var fail in new[] { false, true })
                {
                    using var server = new BundleHttpServer(body);
                    var terminalCalls = 0;
                    var retry = new LoadRemoteBundleTask(server.Url, null, _ => terminalCalls++);
                    try
                    {
                        var first = ObserveResourceCleanup(retry);
                        var second = ObserveResourceCleanup(retry);
                        LiteRuntime.Task.AddTask(retry, LiteConst.TaskGroupId.Asset);
                        retry.Execute();
                        await WaitWithoutLoaderTick(() => GetDownloadField<ulong>(retry, "_retryTimerID") != 0 || retry.IsDone || server.Work.IsFaulted,
                            "Real HTTP timeout creates a retry timer");
                        var timerId = GetDownloadField<ulong>(retry, "_retryTimerID");
                        var timer = LiteRuntime.Timer.FindTimer(timerId);
                        Check(!server.Work.IsFaulted && retry.State == TaskState.Running && timer != null && !timer.IsDone &&
                            GetDownloadField<UnityWebRequest>(retry, "_request") == null,
                            "A real timed-out transfer disposes its first request and waits on an actual retry timer.");
                        var error = new InvalidOperationException("Controlled remote failure during retry delay");
                        if (fail) retry.Fail(error);
                        else retry.Cancel();
                        await WaitForResourceCleanup(first, second, "Retry-delay terminal cleanup");
                        Check(await retry.Task == null && TaskSnapshot().Contains(retry) &&
                            GetDownloadField<ulong>(retry, "_retryTimerID") == 0 && timer.IsDone,
                            "Terminating during the retry delay stops its timer and reports cleanup before scheduler retirement.");
                        timer.Tick(31f);
                        await WaitWithoutLoaderTick(() => !TaskSnapshot().Contains(retry), "Retry-delay wrapper retirement");
                        await ObserveResourceCleanup(retry);
                        Check(GetDownloadField<ulong>(retry, "_retryTimerID") == 0 &&
                            GetDownloadField<UnityWebRequest>(retry, "_request") == null && retry.State == (fail ? TaskState.Failed : TaskState.Cancelled) &&
                            retry.Result == null && ReferenceEquals(retry.Error, fail ? error : null) && terminalCalls == (fail ? 1 : 0) && await retry.Task == null,
                            "An overdue retry tick cannot restart downloading or rewrite either terminal outcome.");
                    }
                    finally
                    {
                        try { await RetireRemoteFixture(retry); }
                        finally
                        {
                            server.Dispose();
                            await WaitWithoutLoaderTick(() => server.Work.IsCompleted, "Retry HTTP fixture socket cleanup");
                            Check(!server.Work.IsFaulted, "Retry HTTP fixture closes its socket worker without an unhandled error.");
                        }
                    }
                }
            }
            finally
            {
                LiteRuntime.Setting.Asset.BundleDownloadRetry = previousRetry;
            }
        }

        private static async UniTask HandleStatusProtocol()
        {
            var h = New();
            var rejectedSuccess = false;
            var rejectedFailure = false;
            try { AssetLoadResult<IAssetLease<TextAsset>>.Success(null); }
            catch (ArgumentNullException) { rejectedSuccess = true; }
            try { AssetLoadResult<IAssetLease<TextAsset>>.Failure(null); }
            catch (ArgumentNullException) { rejectedFailure = true; }
            Check(rejectedSuccess && rejectedFailure,
                "Loading outcomes reject success without a payload and failure without an error.");
            using var cts = new CancellationTokenSource();
            var live = h.Asset<TextAsset>(TextPath, cts.Token);
            Check(live.State == AssetHandleState.Pending && !live.IsDone && !live.IsDisposed && live.Error == null,
                "A new request is pending without an error or a released owner.");
            await Wait(() => live.IsDone, "Handle success state");
            await live;
            AssetLoadResult<IAssetLease<TextAsset>> firstDelivery = null;
            AssetLoadResult<IAssetLease<TextAsset>> secondDelivery = null;
            h.Provider.LoadAssetAsync<TextAsset>(TextPath, AssetLoadPriority.Normal, result => firstDelivery = result);
            h.Provider.LoadAssetAsync<TextAsset>(TextPath, AssetLoadPriority.Normal, result => secondDelivery = result);
            Check(firstDelivery.Succeeded && secondDelivery.Succeeded && firstDelivery.Error == null && secondDelivery.Error == null &&
                firstDelivery.Value.Value == live.Result && secondDelivery.Value.Value == live.Result &&
                !ReferenceEquals(firstDelivery.Value, secondDelivery.Value) && h.AssetRefs("text.ab", TextPath) == 3,
                "Shared cached resources deliver successful outcomes with a distinct owned Lease per request.");
            firstDelivery.Value.Dispose();
            secondDelivery.Value.Dispose();
            Check(firstDelivery.Succeeded && firstDelivery.Value.Value == null && h.AssetRefs("text.ab", TextPath) == 1,
                "Outcome wrappers do not own resources or change history when the receiving owner releases its Lease.");
            cts.Cancel();
            Check(live.State == AssetHandleState.Succeeded && live.Result != null && live.Error == null,
                "Cancellation after success does not change the terminal state or release the resource.");
            live.Dispose(); live.Dispose();
            Check(live.State == AssetHandleState.Succeeded && live.IsDone && live.IsDisposed && live.Result == null,
                "Repeated Dispose releases ownership while retaining successful history.");

            var failed = h.Asset<GameObject>(TextPath);
            var task = Get<BaseTask>(h.Entry("text.ab", TextPath, typeof(GameObject)), "_loadAssetTask");
            await Wait(() => failed.IsDone, "Asset failure state");
            await failed;
            var error = failed.Error;
            Check(failed.State == AssetHandleState.Failed && failed.Result == null &&
                error.ResourceKey == TextPath && ReferenceEquals(error.Exception, task.Error) && error.Exception != null &&
                error.Message == task.Error.Message,
                "Native asset failure completes normally and preserves its original Task exception and resource key.");
            failed.Dispose();
            Check(failed.IsDisposed && failed.State == AssetHandleState.Failed && ReferenceEquals(failed.Error, error),
                "Failure information remains available after Dispose.");

            using var preCancelled = new CancellationTokenSource();
            preCancelled.Cancel();
            var cancelled = h.Asset<TextAsset>(SecondTextPath, preCancelled.Token);
            var didCancel = false;
            try { await cancelled; } catch (OperationCanceledException) { didCancel = true; }
            Check(didCancel && cancelled.State == AssetHandleState.Cancelled && cancelled.IsDone &&
                !cancelled.IsDisposed && cancelled.Error == null && !h.HasAssetEntry("text.ab", SecondTextPath),
                "Pre-cancellation publishes Cancelled without starting a request or claiming disposal.");

            var missing = h.Asset<TextAsset>("assets/__session_validation/not-in-manifest.txt");
            await missing;
            Check(missing.State == AssetHandleState.Failed && missing.Error.ResourceKey == "assets/__session_validation/not-in-manifest.txt" &&
                !string.IsNullOrWhiteSpace(missing.Error.Message) && missing.Error.Exception == null,
                "Missing manifest entries preserve their resource key and failure explanation without inventing an exception.");
            await h.Shutdown();
            var closed = h.Asset<TextAsset>(TextPath);
            await closed;
            Check(closed.State == AssetHandleState.Failed && closed.Error.ResourceKey == TextPath &&
                !string.IsNullOrWhiteSpace(closed.Error.Message) && closed.Error.Exception == null,
                "Requests after shutdown fail normally with an explanation rather than cancellation.");
        }

        private static async UniTask SharedCancellationSuccess()
        {
            var h = New(true);
            var b = h.Asset<TextAsset>(TextPath);
            using var cts = new CancellationTokenSource();
            var a = h.Asset<TextAsset>(TextPath, cts.Token);
            cts.Cancel();
            var cancelled = false;
            try { await a; } catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled && a.IsDone && h.AssetRefs("text.ab", TextPath) == 2,
                "Cancellation ends waiting immediately while both pending request leases remain.");
            var releasedBeforeA = false;
            async UniTask ReleaseB()
            {
                await b;
                Check(b.State == AssetHandleState.Succeeded && b.Result != null && b.Error == null,
                    "Success state and resource are visible before its continuation executes.");
                b.Dispose();
                h.Assets.UnloadUnusedAssets();
                releasedBeforeA = h.Has("text.ab") && h.AssetRefs("text.ab", TextPath) == 1;
            }
            var continuation = ReleaseB();
            h.Provider.Tick(0);
            h.Finish("text.ab", true);
            await Wait(() => h.AssetRefs("text.ab", TextPath) == 0, "Shared late success release");
            await continuation;
            Check(releasedBeforeA && b.State == AssetHandleState.Succeeded && b.IsDisposed &&
                a.State == AssetHandleState.Cancelled && a.Error == null && a.Result == null && h.Refs("text.ab") == 0,
                "Cancelled A protects the handoff, rejects its late Lease and cannot overwrite either terminal state.");
            a.Dispose();
            Check(a.IsDisposed && a.State == AssetHandleState.Cancelled, "Dispose after token cancellation preserves Cancelled.");
        }

        private static async UniTask SharedCancellationFailureRetry()
        {
            var h = New(true);
            var b = h.Asset<TextAsset>(TextPath);
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
                b2 = h.Asset<TextAsset>(TextPath);
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
            h.Finish("text.ab", true);
            await Wait(() => b2.IsDone, "Immediate shared retry success");
            Check(b2.State == AssetHandleState.Succeeded && b2.Result != null && b2.Error == null &&
                b.State == AssetHandleState.Failed && b.IsDisposed && ReferenceEquals(b.Error, oldError),
                "Successful retry neither replaces the old error nor revives the failed Handle.");
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
            var emptyPrefab = h.Asset<GameObject>(PrefabPath);
            var survivingPrefab = h.Asset<GameObject>(PrefabPath);
            var empty = new GameObjectHandle(emptyPrefab, _ => null, default);
            h.Handles.Add(empty);
            Check(!empty.IsDone && !emptyPrefab.IsDone,
                "Null instantiation starts with a pending prefab so its continuation observes the delivery boundary.");
            async UniTask ObserveEmpty()
            {
                await empty;
                Check(empty.State == AssetHandleState.Failed && !empty.IsDisposed && empty.Result == null &&
                    empty.Error != null && empty.Error.ResourceKey == PrefabPath &&
                    empty.Error.Message == "Instantiation returned no GameObject" && empty.Error.Exception == null &&
                    emptyPrefab.IsDisposed && !survivingPrefab.IsDisposed && h.AssetRefs("a.ab", PrefabPath, typeof(GameObject)) == 1,
                    "Null instantiation completes normally and releases its prefab before the continuation without releasing another request.");
            }
            var emptyObservation = ObserveEmpty();
            await Wait(() => empty.IsDone, "Null instantiation completion");
            await emptyObservation;
            var emptyError = empty.Error;
            await Wait(() => survivingPrefab.IsDone, "Surviving prefab after null instantiation");
            empty.Dispose(); empty.Dispose();
            Check(empty.State == AssetHandleState.Failed && empty.IsDisposed && ReferenceEquals(empty.Error, emptyError) &&
                survivingPrefab.Result != null && h.AssetRefs("a.ab", PrefabPath, typeof(GameObject)) == 1,
                "Repeated disposal preserves the null-instantiation failure and the surviving prefab ownership.");
            survivingPrefab.Dispose();
            Check(h.Refs("a.ab") == 0,
                "The final surviving prefab release leaves no ownership from the failed null instantiation.");

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

        private static async UniTask PreloadErrorProtocol()
        {
            var h = New();
            var missing = "assets/__session_validation/not-in-manifest.txt";
            var absentAsset = "assets/__session_validation/missing.txt";
            var preload = h.Assets.PreloadAssetHandle<TextAsset>(new[] { TextPath, missing, absentAsset });
            h.Handles.Add(preload);
            await Wait(() => preload.IsDone, "Preload error details");
            await preload;
            Check(preload.State == AssetHandleState.Failed && !preload.Result && preload.LoadedCount == 1 &&
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

        private static async UniTask SharedAsset()
        {
            var h = New();
            var a = h.Asset<TextAsset>(TextPath); var b = h.Asset<TextAsset>(TextPath); var c = h.Asset<TextAsset>(TextPath);
            await Wait(() => a.IsDone && b.IsDone && c.IsDone, "Three asset handles");
            Check(a.Result != null && a.Result == b.Result && b.Result == c.Result, "Three handles share a valid asset.");
            Check(h.AssetRefs("text.ab", TextPath) == 3 && h.Refs("text.ab") == 1, "Asset refs=3; bundle refs=1.");
            a.Dispose(); a.Dispose(); b.Dispose(); h.Assets.UnloadUnusedAssets();
            Check(c.Result != null && h.AssetRefs("text.ab", TextPath) == 1, "Partial and repeated disposal preserve remaining handle.");
            c.Dispose();
            Check(h.Refs("text.ab") == 0 && h.Entry("text.ab", TextPath).Stage == AssetCacheStage.Loaded &&
                h.Cache("text.ab").IsLoaded && h.Cache("text.ab").IsRetained, "Idle object index remains while its loaded bundle starts retention without an entry lease.");
            h.Assets.UnloadUnusedAssets();
            Check(!h.Has("text.ab"), "Explicit cleanup removes asset and bundle.");
        }

        private static async UniTask Retention()
        {
            var h = New(); var a = h.Asset<TextAsset>(TextPath);
            await Wait(() => a.IsDone, "Retention load");
            var entry = h.Entry("text.ab", TextPath);
            var asset = a.Result;
            Check(!h.Cache("text.ab").TryUnloadUnused(true) && asset != null && entry.Stage == AssetCacheStage.Loaded,
                "TryUnloadUnused never releases a live owner's asset, even when retention is ignored.");
            h.Provider.Tick(1000);
            Check(a.Result != null && h.Refs("text.ab") == 1, "Live Handle protects the bundle beyond its retention duration.");
            a.Dispose();
            Check(h.Refs("text.ab") == 0 && entry.Stage == AssetCacheStage.Loaded &&
                Get<AssetBundleCache.Lease>(entry, "_mainLease") == null, "Last release immediately returns Bundle lease and preserves object index.");
            Check(!h.Cache("text.ab").TryUnloadUnused(false) && entry.Asset == asset,
                "TryUnloadUnused respects an unexpired retention period without changing the cached object.");
            h.Provider.Tick(299);
            Check(h.Has("text.ab") && entry.Asset == asset && h.Cache("text.ab").IsLoaded && h.Cache("text.ab").IsRetained,
                "Bundle and original index remain before 300 seconds.");
            var again = h.Asset<TextAsset>(TextPath);
            Check(again.IsDone && again.Result == asset && ReferenceEquals(entry, h.Entry("text.ab", TextPath)) &&
                h.Refs("text.ab") == 1 && h.Cache("text.ab").Stage == AssetCacheStage.Loaded &&
                h.Locater.Calls["text.ab"] == 1, "Retained object is delivered synchronously from the same entry with renewed Bundle ownership.");
            var remainingRetainTime = Get<float>(h.Cache("text.ab"), "_retainTime");
            h.Provider.Tick(1000);
            Check(h.Has("text.ab") && again.Result == asset && !h.Cache("text.ab").IsRetained &&
                Get<float>(h.Cache("text.ab"), "_retainTime") == remainingRetainTime,
                "Reacquiring the retained bundle pauses its timer while the Handle remains active.");
            again.Dispose(); h.Provider.Tick(299);
            Check(h.Has("text.ab"), "New last release restarts the full Bundle retention period.");
            h.Provider.Tick(2);
            Check(!h.Has("text.ab") && entry.Stage == AssetCacheStage.Unloaded && entry.Asset == null && asset == null,
                "Bundle expiry clears the old index and destroys its native object.");
            var fresh = h.Asset<TextAsset>(TextPath);
            await Wait(() => fresh.IsDone, "Load after Bundle expiry");
            Check(fresh.Result != null && !ReferenceEquals(entry, h.Entry("text.ab", TextPath)) && h.Locater.Calls["text.ab"] == 2,
                "Request after expiry reloads Bundle and creates a fresh index.");
            var freshCache = h.Cache("text.ab");
            var freshEntry = h.Entry("text.ab", TextPath);
            var freshAsset = fresh.Result;
            fresh.Dispose();
            Check(freshCache.TryUnloadUnused(true) && freshCache.Stage == AssetCacheStage.Unloaded && freshCache.Bundle == null &&
                freshEntry.Stage == AssetCacheStage.Unloaded && freshEntry.Asset == null && freshAsset == null,
                "Ignoring retention synchronously unloads an idle Bundle and invalidates its child asset index.");
            Check(!freshCache.TryUnloadUnused(true), "An already-unloaded cache cannot be reclaimed a second time.");
        }

        private static async UniTask SharedBundleRetention()
        {
            var h = New();
            var first = h.Asset<TextAsset>(TextPath);
            var second = h.Asset<TextAsset>(SecondTextPath);
            await Wait(() => first.IsDone && second.IsDone, "Two distinct assets in one bundle");
            var firstEntry = h.Entry("text.ab", TextPath);
            var secondEntry = h.Entry("text.ab", SecondTextPath);
            var firstObject = first.Result;
            Check(firstObject != null && second.Result != null && h.Refs("text.ab") == 2,
                "Two active entries each own one lease on their shared bundle.");
            first.Dispose();
            h.Provider.Tick(1000);
            Check(h.Refs("text.ab") == 1 && firstEntry.Asset == firstObject &&
                firstEntry.Stage == AssetCacheStage.Loaded && Get<AssetBundleCache.Lease>(firstEntry, "_mainLease") == null,
                "Idle index has no timer or lease and survives while a sibling asset protects the bundle.");
            var again = h.Asset<TextAsset>(TextPath);
            Check(again.IsDone && again.Result == firstObject && h.Refs("text.ab") == 2,
                "Idle sibling index synchronously reacquires its Bundle lease.");
            again.Dispose(); second.Dispose();
            Check(h.Refs("text.ab") == 0 && h.Cache("text.ab").IsLoaded && h.Cache("text.ab").IsRetained,
                "Bundle retention begins only after both entries become idle.");
            h.Provider.Tick(301);
            Check(!h.Has("text.ab") && firstEntry.Asset == null && secondEntry.Asset == null &&
                firstEntry.Stage == AssetCacheStage.Unloaded && secondEntry.Stage == AssetCacheStage.Unloaded,
                "One Bundle unload clears both idle indexes.");
        }

        private static async UniTask DependencyRetention()
        {
            var h = New();
            var handle = h.Asset<GameObject>(PrefabPath);
            await Wait(() => handle.IsDone, "Dependency retention prefab");
            var prefab = handle.Result;
            var material = prefab.GetComponent<Renderer>().sharedMaterial;
            handle.Dispose();
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

        private static async UniTask AssetSystemDisposeSubmitted()
        {
            var h = New(true);
            var handle = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0); h.Finish("text.ab", true);
            var bundle = h.Cache("text.ab");
            var entry = h.Entry("text.ab", TextPath);
            var task = Get<BaseTask>(entry, "_loadAssetTask");
            var completions = 0;
            entry.LoadAssetAsync(entry.AcquireLease<TextAsset>(), result => { completions++; result.Value?.Dispose(); });
            task.Execute();
            Check(task.IsExecuted && !handle.IsDone && h.Refs("text.ab") == 1,
                "Real native asset request is submitted before system shutdown.");
            await h.Shutdown();
            Check(handle.IsDone && handle.Result == null && completions == 1 && task.IsDone,
                "System shutdown ends all pending asset deliveries and cancels their task.");
            Check(entry.Stage == AssetCacheStage.Unloaded && entry.Asset == null &&
                Get<AssetBundleCache.Lease>(entry, "_mainLease") == null &&
                bundle.Stage == AssetCacheStage.Unloaded && Get<int>(bundle, "_refCount") == 0,
                "Shutdown returns the loading lease and clears entry and bundle ownership.");
            handle.Dispose(); await entry.DisposeAsync();
            for (var i = 0; i < 4; i++) await Frame();
            Check(completions == 1 && handle.Result == null && entry.Asset == null &&
                Get<int>(bundle, "_refCount") == 0, "Repeated disposal and late native completion do not revive or release entries twice.");
        }

        private static async UniTask ShutdownFailureRetry(bool differentBundle)
        {
            var h = New(true);
            var first = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0);
            var nativeBundle = h.Finish("text.ab", true);
            var cache = h.Cache("text.ab");
            var entry = h.Entry("text.ab", TextPath);
            var task = Get<BaseTask>(entry, "_loadAssetTask");
            task.Execute();
            Check(task.IsExecuted && !first.IsDone && nativeBundle != null,
                "Real Bundle is loaded and its native Asset request is outstanding before shutdown.");

            AssetHandle<UnityEngine.Object> retry = null;
            var retries = 0;
            Exception shutdownError = null;
            Exception retryError = null;
            async UniTask Retry()
            {
                await first;
                retries++;
                retry = h.Asset<UnityEngine.Object>(differentBundle ? PrefabPath : SecondTextPath);
            }
            var continuation = Retry();
            try
            {
                try { await h.Shutdown(); }
                catch (Exception ex) { shutdownError = ex; }
                try { await continuation; }
                catch (Exception ex) { retryError = ex; }

                var assets = Get<Dictionary<(string, Type), AssetInfoCache>>(cache, "_assetCacheMap");
                var bundles = Get<Dictionary<string, AssetBundleCache>>(h.Provider, "_bundleCacheMap");
                _case.evidence.Add("Shutdown exception: " + (shutdownError?.ToString() ?? "none"));
                _case.evidence.Add("Retry exception: " + (retryError?.ToString() ?? "none"));
                _case.evidence.Add("Observed before harness cleanup: cache=" + cache.Stage +
                    ", nativeBundleAlive=" + (nativeBundle != null) +
                    ", assetEntries=" + assets.Count +
                    ", providerEntries=" + bundles.Count +
                    ", retryCreated=" + (retry != null) +
                    ", retryIsDone=" + (retry?.IsDone ?? false) +
                    ", retries=" + retries);
                Check(shutdownError == null && retryError == null,
                    "Shutdown and its failure retry finish without throwing or aborting cache enumeration.");
                Check(retries == 1 && first.IsDone && first.Result == null &&
                    retry != null && retry.IsDone && retry.Result == null,
                    "Retry is rejected through a normal completed failure result.");
                Check(cache.Stage == AssetCacheStage.Unloaded && nativeBundle == null &&
                    assets.Count == 0 && bundles.Count == 0,
                    "Shutdown completes native Bundle unloading and clears both cache dictionaries.");
            }
            finally
            {
                if (nativeBundle != null)
                {
                    _case.evidence.Add("Harness-only cleanup of leaked native Bundle after recording the shutdown regression.");
                    nativeBundle.Unload(true);
                }
                if (shutdownError != null)
                {
                    _case.evidence.Add("Harness-only second Provider.Dispose after recording aborted Provider enumeration.");
                    await h.Provider.DisposeAsync();
                }
            }
        }

        private static async UniTask CacheShutdownRequestGuards()
        {
            var h = New(true);
            var first = h.Asset<TextAsset>(TextPath);
            h.Provider.Tick(0);
            var native = h.Finish("text.ab", true);
            var cache = h.Cache("text.ab");
            var entries = Get<Dictionary<(string, Type), AssetInfoCache>>(cache, "_assetCacheMap");
            var scenes = Get<List<SceneInfoCache>>(cache, "_sceneCacheMap");
            var assetCallbacks = 0;
            var sceneCallbacks = 0;
            var unexpectedLease = false;
            void RequestBoth()
            {
                var assetCount = entries.Count;
                var sceneCount = scenes.Count;
                cache.LoadAssetAsync<TextAsset>(SecondTextPath, AssetLoadPriority.Normal, result =>
                {
                    assetCallbacks++;
                    unexpectedLease |= result.Value != null;
                    result.Value?.Dispose();
                    Check(!result.Succeeded && result.Value == null && result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message),
                        "Rejected cache requests deliver a failure outcome with no owned Lease.");
                });
                cache.LoadSceneAsync("validation_scene", new LoadSceneParameters(LoadSceneMode.Additive), AssetLoadPriority.Normal, result =>
                {
                    sceneCallbacks++;
                    unexpectedLease |= result.Value != null;
                    result.Value?.Dispose();
                    Check(!result.Succeeded && result.Value == null && result.Error != null && !string.IsNullOrWhiteSpace(result.Error.Message),
                        "Rejected cache requests deliver a failure outcome with no owned Lease.");
                });
                Check(entries.Count == assetCount && scenes.Count == sceneCount && !unexpectedLease,
                    "Rejected cache requests do not create Asset/Scene entries or acquire leases at stage " + cache.Stage + ".");
            }
            async UniTask RetryDuringUnload()
            {
                await first;
                Check(cache.Stage == AssetCacheStage.Unloading, "Direct requests target the cache while its unload traversal is active.");
                RequestBoth();
            }
            var continuation = RetryDuringUnload();
            await h.Shutdown();
            await continuation;
            Check(cache.Stage == AssetCacheStage.Unloaded && native == null && !h.Has("text.ab"),
                "Direct rejected requests do not interrupt native Bundle unloading or Provider cleanup.");
            RequestBoth();
            Check(assetCallbacks == 2 && sceneCallbacks == 2 && entries.Count == 0 && scenes.Count == 0,
                "Unloading and Unloaded requests each receive exactly one normal failure notification.");
        }

        private static async UniTask PendingRequestLeases()
        {
            var h = New(true);
            using var cts = new CancellationTokenSource();
            var first = h.Asset<TextAsset>(TextPath);
            var second = h.Asset<TextAsset>(TextPath, cts.Token);
            Check(!first.IsDone && !second.IsDone && h.AssetRefs("text.ab", TextPath) == 2 &&
                h.Refs("text.ab") == 1, "Both requests own Asset leases and one shared Bundle lease before Bundle delivery.");
            first.Dispose();
            cts.Cancel();
            var canceled = 0;
            try { await first; } catch (OperationCanceledException) { canceled++; }
            try { await second; } catch (OperationCanceledException) { canceled++; }
            h.Assets.UnloadUnusedAssets();
            Check(canceled == 2 && h.Has("text.ab") && h.AssetRefs("text.ab", TextPath) == 2,
                "Dispose and cancellation end waiting but preserve pending request leases.");
            h.Provider.Tick(0);
            h.Finish("text.ab", true);
            await Wait(() => h.AssetRefs("text.ab", TextPath) == 0, "Canceled requests finish delivery");
            Check(first.Result == null && second.Result == null && h.AssetRefs("text.ab", TextPath) == 0 &&
                h.Refs("text.ab") == 0, "Late results return both request leases and the shared Bundle lease.");
            h.Assets.UnloadUnusedAssets();
            Check(!h.Has("text.ab"), "Canceled request cleanup removes the Bundle.");
        }

        private static async UniTask FinalContinuationReload()
        {
            var h = New();
            var first = h.Asset<TextAsset>(TextPath);
            var originalBundle = h.Cache("text.ab");
            var originalEntry = h.Entry("text.ab", TextPath);
            AssetHandle<TextAsset> second = null;
            async UniTask Reload()
            {
                await first;
                var originalAsset = first.Result;
                Check(originalAsset != null, "First continuation receives a real asset.");
                first.Dispose();
                Check(Get<int>(originalEntry, "_refCount") == 0 && h.Refs("text.ab") == 0,
                    "Final release immediately returns both reference levels inside the continuation.");
                h.Assets.UnloadUnusedAssets();
                Check(!h.Has("text.ab") && originalBundle.Stage == AssetCacheStage.Unloaded &&
                    originalEntry.Stage == AssetCacheStage.Unloaded && originalAsset == null,
                    "Immediate cleanup unloads the old Bundle and destroys the native asset before the continuation returns.");
                second = h.Asset<TextAsset>(TextPath);
                Check(!ReferenceEquals(originalBundle, h.Cache("text.ab")) &&
                    !ReferenceEquals(originalEntry, h.Entry("text.ab", TextPath)) && h.AssetRefs("text.ab", TextPath) == 1,
                    "Same-path reload owns a new Bundle, new Asset entry and independent pending Lease.");
            }
            var continuation = Reload();
            await Wait(() => first.IsDone, "First load before immediate cleanup");
            await continuation;
            await Wait(() => second.IsDone, "Same-path reload after cleanup");
            Check(second.Result != null && h.Refs("text.ab") == 1 &&
                originalBundle.Stage == AssetCacheStage.Unloaded, "Old completion does not release or revive the new cache.");
            second.Dispose();
            h.Assets.UnloadUnusedAssets();
            Check(!h.Has("text.ab"), "Reloaded asset releases normally.");
        }

        private static async UniTask AssetFailureCleanupRetry()
        {
            var h = New();
            var first = h.Asset<GameObject>(TextPath);
            var originalBundle = h.Cache("text.ab");
            AssetHandle<TextAsset> second = null;
            async UniTask Retry()
            {
                await first;
                Check(first.Result == null && h.AssetRefs("text.ab", TextPath, typeof(GameObject)) == 0 && h.Refs("text.ab") == 0,
                    "Wrong-type native load fails and returns its pending Lease before notifying the caller.");
                h.Assets.UnloadUnusedAssets();
                Check(!h.Has("text.ab") && originalBundle.Stage == AssetCacheStage.Unloaded,
                    "Asset failure continuation can immediately reclaim its old Bundle.");
                second = h.Asset<TextAsset>(TextPath);
                Check(!ReferenceEquals(originalBundle, h.Cache("text.ab")),
                    "Correct-type retry of the same path creates a new cache.");
            }
            var continuation = Retry();
            await Wait(() => first.IsDone, "Wrong-type native asset failure");
            await continuation;
            await Wait(() => second.IsDone, "Same-path retry after asset failure cleanup");
            Check(second.Result != null && h.AssetRefs("text.ab", TextPath) == 1 && h.Refs("text.ab") == 1,
                "Retry survives the previous failed Asset completion.");
        }

        private static async UniTask BundleFailureCleanupRetry()
        {
            var h = New(true);
            var first = h.Asset<TextAsset>(TextPath);
            var originalBundle = h.Cache("text.ab");
            AssetHandle<TextAsset> second = null;
            async UniTask Retry()
            {
                await first;
                Check(first.Result == null && h.Refs("text.ab") == 0, "Bundle failure returns the request's Bundle ownership.");
                h.Assets.UnloadUnusedAssets();
                Check(!h.Has("text.ab") && originalBundle.Stage == AssetCacheStage.Unloaded,
                    "Bundle failure continuation removes its old cache immediately.");
                second = h.Asset<TextAsset>(TextPath);
                Check(!ReferenceEquals(originalBundle, h.Cache("text.ab")) && h.Refs("text.ab") == 1,
                    "Same-path retry owns an independent cache while the old Bundle callback is still on the stack.");
            }
            var continuation = Retry();
            h.Provider.Tick(0);
            h.Finish("text.ab", false);
            await continuation;
            Check(!second.IsDone, "Old failure does not prematurely complete the new request.");
            h.Finish("text.ab", true);
            await Wait(() => second.IsDone, "Retry after Bundle failure cleanup");
            Check(second.Result != null && h.Refs("text.ab") == 1, "Real retry result retains exactly one Bundle reference.");
        }

        private static async UniTask SharedFailureRetry()
        {
            var h = New(true);
            var first = h.Asset<TextAsset>(TextPath);
            var second = h.Asset<TextAsset>(TextPath);
            AssetHandle<TextAsset> retry = null;
            var interleaved = false;
            async UniTask RetryFirst()
            {
                await first;
                interleaved = !second.IsDone;
                retry = h.Asset<TextAsset>(TextPath);
                Check(h.AssetRefs("text.ab", TextPath) == 2,
                    "New retry and remaining old request both own pending Asset leases.");
            }
            async UniTask CleanSecond()
            {
                await second;
                h.Assets.UnloadUnusedAssets();
                Check(retry != null && !retry.IsDone && h.Has("text.ab") &&
                    h.AssetRefs("text.ab", TextPath) == 1 && h.Refs("text.ab") == 1,
                    "Last old failure cannot reclaim the new retry's cache.");
            }
            var retryContinuation = RetryFirst();
            var cleanupContinuation = CleanSecond();
            h.Provider.Tick(0);
            h.Finish("text.ab", false);
            await retryContinuation;
            await cleanupContinuation;
            Check(interleaved && first.Result == null && second.Result == null,
                "Retry ran between the two old failure notifications.");
            h.Finish("text.ab", true);
            await Wait(() => retry.IsDone, "Retry after shared failure");
            Check(retry.Result != null && h.AssetRefs("text.ab", TextPath) == 1, "New completion delivers its own successful result.");
        }

        private static async UniTask ProfilerExceptionIsolation()
        {
            var h = New();
            var eventCount = 0;
            void ThrowOnEvent(AssetLoadEventArgs args)
            {
                eventCount++;
                throw new InvalidOperationException("Injected validation profiler failure");
            }
            AssetLoadEventDispatcher.OnLoadEvent += ThrowOnEvent;
            try
            {
                var handle = h.Asset<TextAsset>(TextPath);
                await Wait(() => handle.IsDone, "Load with throwing Profiler observer");
                Check(handle.Result != null && eventCount >= 6 && h.AssetRefs("text.ab", TextPath) == 1 &&
                    h.Refs("text.ab") == 1, "Session, Bundle and Asset event exceptions do not interrupt Lease transfer.");
                handle.Dispose();
                h.Assets.UnloadUnusedAssets();
                Check(!h.Has("text.ab"), "Profiler exceptions leave no orphaned request or Bundle reference.");
            }
            finally
            {
                AssetLoadEventDispatcher.OnLoadEvent -= ThrowOnEvent;
            }
        }

        private static async UniTask ReentrantAsset()
        {
            var h = New(); var a = h.Asset<TextAsset>(TextPath); var b = h.Asset<TextAsset>(TextPath);
            var interleaved = false;
            async UniTask ContinueFirst()
            {
                await a;
                interleaved = !b.IsDone;
                a.Dispose(); h.Assets.UnloadUnusedAssets();
            }
            var continuation = ContinueFirst();
            await Wait(() => a.IsDone && b.IsDone, "Reentrant asset completion"); await continuation;
            Check(interleaved, "Cleanup actually ran before second handle completed.");
            Check(b.Result != null && h.AssetRefs("text.ab", TextPath) == 1 && h.Refs("text.ab") == 1, "Second result and ownership survived cleanup.");
        }

        private static async UniTask DisposeDuringAsset()
        {
            var h = New(); var a = h.Asset<TextAsset>(TextPath);
            await Wait(() => h.HasAssetEntry("text.ab", TextPath) && h.Entry("text.ab", TextPath).Stage == AssetCacheStage.Loading, "Asset queued");
            var task = Get<BaseTask>(h.Entry("text.ab", TextPath), "_loadAssetTask");
            if (!task.IsExecuted) task.Execute();
            Check(task.IsExecuted && !a.IsDone, "Real asset request submitted before Handle disposal.");
            a.Dispose();
            var canceled = false; try { await a; } catch (OperationCanceledException) { canceled = true; }
            Check(a.State == AssetHandleState.Cancelled && a.IsDone && a.IsDisposed && a.Error == null,
                "Dispose immediately publishes cancellation without an error.");
            await Wait(() => h.AssetRefs("text.ab", TextPath) == 0, "Late asset completion");
            Check(canceled && a.Result == null && h.AssetRefs("text.ab", TextPath) == 0, "Late asset result released after disposal.");
        }

        private static async UniTask CancelDuringAsset()
        {
            var h = New(); using var cts = new CancellationTokenSource();
            var a = h.Asset<TextAsset>(TextPath, cts.Token); cts.Cancel();
            var canceled = false; try { await a; } catch (OperationCanceledException) { canceled = true; }
            Check(a.State == AssetHandleState.Cancelled && a.IsDone && !a.IsDisposed && a.Error == null,
                "Token cancellation immediately ends the request without marking it disposed.");
            await Wait(() => h.AssetRefs("text.ab", TextPath) == 0, "Late canceled asset");
            Check(canceled && a.Result == null && h.AssetRefs("text.ab", TextPath) == 0, "Cancellation does not leak successful late reference.");
        }

        private static async UniTask TypeMismatch()
        {
            var h = New();
            var a = h.Asset<TextAsset>(TextPath);
            await Wait(() => a.IsDone, "Type warmup");
            var entry = h.Entry("text.ab", TextPath);
            var asset = a.Result;
            var wrong = h.Asset<GameObject>(TextPath);
            await Wait(() => wrong.IsDone, "Independent wrong-type request");
            var wrongEntry = h.Entry("text.ab", TextPath, typeof(GameObject));
            Check(wrong.Result == null && h.AssetRefs("text.ab", TextPath) == 1 &&
                Get<int>(wrongEntry, "_refCount") == 0 && !ReferenceEquals(entry, wrongEntry),
                "Wrong-type load fails in its own typed cache and preserves the valid request.");
            a.Dispose();
            Check(h.Refs("text.ab") == 0 && Get<AssetBundleCache.Lease>(entry, "_mainLease") == null,
                "Both typed entries return their Bundle leases when idle.");
            var wrongIdle = h.Asset<GameObject>(TextPath);
            await Wait(() => wrongIdle.IsDone, "Idle wrong-type retry");
            Check(wrongIdle.Result == null && h.Refs("text.ab") == 0 && entry.Asset == asset &&
                Get<int>(wrongEntry, "_refCount") == 0,
                "Wrong-type retry leaves no references and does not damage the retained valid typed index.");
            var correct = h.Asset<TextAsset>(TextPath);
            Check(correct.IsDone && correct.Result == asset && h.Refs("text.ab") == 1,
                "Correct type synchronously reacquires its own retained object after mismatch.");
        }

        private static async UniTask MissingAsset()
        {
            var h = New(); var a = h.Asset<TextAsset>("assets/__session_validation/missing.txt");
            await Wait(() => a.IsDone, "Missing asset result");
            Check(a.Result == null, "Missing asset completes with null.");
            h.Assets.UnloadUnusedAssets(); Check(!h.Has("text.ab"), "Failed entry and lease can be reclaimed.");
        }

        private static async UniTask RetryAsset()
        {
            var h = New(); var first = h.Asset<TextAsset>("assets/__session_validation/missing.txt");
            AssetHandle<TextAsset> second = null;
            async UniTask Retry() { await first; second = h.Asset<TextAsset>(TextPath); }
            var retry = Retry(); await Wait(() => second != null && second.IsDone, "Failure callback retry"); await retry;
            Check(second.Result != null && h.Refs("text.ab") == 1, "Retry succeeds while the failed entry holds no Bundle lease.");
        }

        private static async UniTask SharedDependency()
        {
            var h = New(); var a = h.Asset<GameObject>(PrefabPath); var c = h.Asset<GameObject>("assets/__session_validation/c.prefab");
            await Wait(() => a.IsDone && c.IsDone, "Shared prefabs");
            Check(a.Result.GetComponent<Renderer>().sharedMaterial != null && h.Refs("b.ab") == 2, "Two real prefabs keep two dependency leases.");
            a.Dispose(); h.Assets.UnloadUnusedAssets();
            Check(c.Result != null && h.Refs("b.ab") == 1, "One parent cleanup preserves other parent dependency.");
            var instance = h.Assets.InstantiateHandle("assets/__session_validation/c.prefab", null); h.Handles.Add(instance);
            await Wait(() => instance.IsDone, "Instantiation"); var go = instance.Result;
            Check(go != null, "GameObjectHandle instantiates from protected prefab.");
            instance.Dispose(); await Frame(); Check(go == null, "Instance destroyed after disposal.");
        }

        private static async UniTask Preload()
        {
            var h = New(); var preload = h.Assets.PreloadAssetHandle<TextAsset>(new[] { TextPath, TextPath }); h.Handles.Add(preload);
            await Wait(() => preload.IsDone, "Preload");
            Check(preload.Result && preload.TotalCount == 1 && h.AssetRefs("text.ab", TextPath) == 1, "Preload deduplicates and owns one request.");
        }

        private static async UniTask SlowParent()
        {
            var h = New(true); var a = h.Asset<GameObject>(PrefabPath); h.Provider.Tick(0); h.Finish("b.ab", true);
            h.Assets.UnloadUnusedAssets(); h.Provider.Tick(1000);
            Check(!a.IsDone && h.Refs("b.ab") == 1 && h.Cache("b.ab").Bundle != null, "Dependency survives cleanup while parent is pending.");
            h.Finish("a.ab", true); await Wait(() => a.IsDone, "Slow parent asset");
            Check(a.Result != null && h.Refs("b.ab") == 1, "Successful parent does not acquire dependency twice.");
        }

        private static async UniTask MainFailure()
        {
            var h = New(true); var a = h.Asset<GameObject>(PrefabPath); h.Provider.Tick(0); h.Finish("a.ab", false);
            Check(!a.IsDone && h.Refs("b.ab") == 1, "Main failure waits for outstanding dependency.");
            h.Finish("b.ab", true); Check(a.IsDone && a.Result == null && h.Refs("b.ab") == 0, "Settled group rolls back dependency lease.");
            await UniTask.Yield();
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

        private static async UniTask SharedFailure()
        {
            var h = New(true); var a = h.Asset<GameObject>(PrefabPath); var c = h.Asset<GameObject>("assets/__session_validation/c.prefab");
            h.Provider.Tick(0); h.Finish("b.ab", true); Check(h.Refs("b.ab") == 2, "Both parents pin shared dependency.");
            h.Finish("a.ab", false); h.Assets.UnloadUnusedAssets(); Check(h.Refs("b.ab") == 1, "Failed parent releases only its own lease.");
            h.Finish("c.ab", true); await Wait(() => c.IsDone, "Surviving parent"); Check(c.Result != null, "Other parent still loads real prefab.");
        }

        private static async UniTask BundleHandoff()
        {
            var h = New(true); var a = h.Asset<GameObject>(PrefabPath); var b = h.Asset<Material>(MaterialPath);
            var interleaved = false;
            async UniTask CleanupOnFailure() { await a; interleaved = !b.IsDone; h.Assets.UnloadUnusedAssets(); }
            var continuation = CleanupOnFailure(); h.Provider.Tick(0); h.Finish("a.ab", false); h.Finish("b.ab", true);
            await Wait(() => b.IsDone, "Direct dependency request"); await continuation;
            Check(interleaved && b.Result != null && h.Refs("b.ab") == 1, "Cleanup during parent failure cannot break pending direct dependency request.");
        }

        private static async UniTask RetryBundle()
        {
            var h = New(true); var first = h.Asset<GameObject>(PrefabPath); AssetHandle<GameObject> second = null;
            async UniTask Retry() { await first; second = h.Asset<GameObject>(PrefabPath); }
            var retry = Retry(); h.Provider.Tick(0); h.Finish("b.ab", true); h.Finish("a.ab", false); await retry;
            Check(second != null && !second.IsDone && h.Refs("b.ab") == 1, "Retry batch retains its new dependency lease.");
            h.Finish("a.ab", true); await Wait(() => second.IsDone, "Retried parent"); Check(second.Result != null, "Retry succeeds.");
        }

        private static async UniTask LoaderFailure()
        {
            var h = New(true); h.Locater.ThrowOnCreate = true;
            var first = h.Asset<TextAsset>(TextPath); AssetHandle<TextAsset> second = null;
            async UniTask Retry() { await first; second = h.Asset<TextAsset>(TextPath); }
            var retry = Retry(); h.Provider.Tick(0); await retry;
            Check(first.IsDone && first.Result == null && second != null && !second.IsDone && h.Locater.Calls["text.ab"] == 1, "Creation failure notifies once; retry is deferred to next Tick.");
            h.Locater.ThrowOnCreate = false; h.Provider.Tick(0); h.Finish("text.ab", true);
            await Wait(() => second.IsDone, "Retry after creation failure"); Check(second.Result != null && h.Locater.Calls["text.ab"] == 2, "Retried locator request succeeds independently.");
        }

        private static async UniTask AssetTaskFailure()
        {
            var calls = 0; var task = new LoadAssetTask<TextAsset>(null, "payload.txt", asset => { calls++; });
            LiteRuntime.Task.AddTask(task); await Wait(() => task.IsDone, "Asset task startup failure");
            task.Cancel(); Check(calls == 1, "Startup exception and repeated Cancel notify once.");
            Check(task.State == TaskState.Failed && task.Error is NullReferenceException && task.Result == null,
                "Startup failure preserves its original exception instead of becoming cancellation.");
        }

        private static async UniTask AssetTaskCancel()
        {
            var calls = 0; var task = new LoadAssetTask<TextAsset>(null, "payload.txt", asset => calls++);
            task.Cancel(); task.Cancel(); task.Dispose(); Check(calls == 1 && !task.IsExecuted, "Pre-execution Cancel reports failure exactly once.");
            await UniTask.Yield();
        }

        private static async UniTask EntryDispose()
        {
            var h = New(); var bundle = AssetBundle.LoadFromFile(Path.Combine(BundleFolder, "text.ab")); h.Locater.Opened.Add(bundle);
            var cache = h.Cache("text.ab"); typeof(AssetBundleCache).GetMethod("OnBundleLoaded", Private).Invoke(cache, new object[] { bundle });
            var entry = new AssetInfoCache(cache, TextPath, typeof(TextAsset)); var calls = 0;
            entry.LoadAssetAsync(entry.AcquireLease<TextAsset>(), result => { calls++; result.Value?.Dispose(); }); await entry.DisposeAsync(); await entry.DisposeAsync();
            Check(entry.Stage == AssetCacheStage.Unloaded && calls == 1 && h.Refs("text.ab") == 0, "Dispose cancels queued task without reviving entry or double notification.");

            var reading = new AssetInfoCache(cache, TextPath, typeof(TextAsset));
            reading.LoadAssetAsync(reading.AcquireLease<TextAsset>(), result => { calls++; result.Value?.Dispose(); });
            var task = (LoadAssetTask<TextAsset>)Get<BaseTask>(reading, "_loadAssetTask");
            task.Execute();
            var request = Get<AssetBundleRequest>(task, "_assetRequest");
            h.Assets.UnloadUnusedAssets();
            Check(bundle != null && task.State == TaskState.Running && reading.Stage == AssetCacheStage.Loading,
                "Ordinary reclamation rejects a native asset read without cancelling its task.");
            var disposal = reading.DisposeAsync();
            Check(disposal.Status == UniTaskStatus.Pending && reading.Stage == AssetCacheStage.Unloading && h.Refs("text.ab") == 1,
                "Asset cache disposal retains its Bundle lease while the cancelled native read is pending.");
            h.Assets.UnloadUnusedAssets();
            Check(h.Has("text.ab") && bundle != null, "Normal cache reclamation cannot unload a Bundle still used by asynchronous disposal.");
            await disposal;
            Check(request.isDone && reading.Stage == AssetCacheStage.Unloaded && h.Refs("text.ab") == 0 && calls == 2,
                "Asset DisposeAsync completes only after native reading and returns its lease once.");
            h.Assets.UnloadUnusedAssets();
            Check(!h.Has("text.ab") && bundle == null, "Idle cache reclamation unloads synchronously and removes the cache from its Provider.");
            await Frame();
        }

        private static async UniTask LeaseAccounting()
        {
            var h = New(); var cache = h.Cache("text.ab"); var lease = cache.AcquireLease();
            Check(!cache.TryUnloadUnused(true) && cache.Stage == AssetCacheStage.Idle && cache.IsUsed,
                "A referenced idle cache cannot be reclaimed, even when retention is ignored.");
            lease.Dispose(); lease.Dispose(); Check(!cache.IsUsed && cache.CanUnload(false) && cache.CanUnload(true), "Idle cache lease returns exactly once.");
            var late = cache.AcquireLease(); await cache.DisposeAsync(); late.Dispose();
            Check(cache.Stage == AssetCacheStage.Unloaded, "Late lease disposal tolerates already-unloaded owner.");
            var unused = new AssetBundleCache(h.Provider, Get<BundleInfo>(cache, "_bundleInfo"));
            Check(unused.TryUnloadUnused(false) && unused.Stage == AssetCacheStage.Unloaded && !unused.TryUnloadUnused(false),
                "An empty unreferenced cache is reclaimed synchronously exactly once.");
            await UniTask.Yield();
        }

        private static async UniTask NoRetention()
        {
            LiteRuntime.Setting.Asset.EnableRetain = false;
            var h = New(); var first = h.Asset<TextAsset>(TextPath); var second = h.Asset<TextAsset>(TextPath);
            var interleaved = false;
            async UniTask ReleaseFirst() { await first; interleaved = !second.IsDone; first.Dispose(); h.Assets.UnloadUnusedAssets(); }
            var continuation = ReleaseFirst(); await Wait(() => second.IsDone, "Zero retention concurrent load"); await continuation;
            Check(interleaved && second.Result != null, "Pending request ownership protects delivery even with zero retention.");
            second.Dispose(); h.Provider.Tick(0);
            Check(!h.Has("text.ab"), "Last release plus normal Tick reclaims zero-retention bundle.");
        }

        private static async UniTask AssetFailurePipeline()
        {
            var h = New(true); var handle = h.Asset<TextAsset>(TextPath); h.Provider.Tick(0);
            var bundle = h.Finish("text.ab", true);
            var entry = h.Entry("text.ab", TextPath);
            var task = Get<BaseTask>(entry, "_loadAssetTask");
            Check(!task.IsExecuted && !handle.IsDone, "Asset task queued before fault injection.");
            bundle.Unload(true);
            await Wait(() => handle.IsDone, "Invalid bundle asset failure notification");
            Check(handle.Result == null && entry.Stage != AssetCacheStage.Loading, "Startup exception reaches Handle and leaves Loading.");
            h.Assets.UnloadUnusedAssets();
            Check(!h.Has("text.ab"), "Failure pipeline releases entry lease and cache.");
        }

        private static async UniTask PreCancelledHandle()
        {
            var h = New(); using var cts = new CancellationTokenSource(); cts.Cancel();
            var handle = h.Asset<TextAsset>(TextPath, cts.Token);
            var canceled = false; try { await handle; } catch (OperationCanceledException) { canceled = true; }
            Check(canceled && handle.IsDone && !h.Has("text.ab") && h.Locater.Calls.Count == 0, "Pre-cancelled Handle creates no cache, task or reference.");
        }

        private static async UniTask MissingDependency()
        {
            var h = New(true); var cache = h.Cache("a.ab");
            Get<BundleInfo>(cache, "_bundleInfo").DependencyList = new[] { "b.ab", "absent.ab" };
            var handle = h.Asset<GameObject>(PrefabPath); h.Provider.Tick(0); h.Finish("b.ab", true);
            Check(!handle.IsDone && h.Refs("b.ab") == 1, "Missing metadata still waits for main and holds valid dependency.");
            var main = h.Finish("a.ab", true);
            Check(handle.IsDone && handle.Result == null && main == null && h.Refs("b.ab") == 0, "Missing dependency causes complete rollback, including main result.");
            await UniTask.Yield();
        }

        private static async UniTask AssetLeaseIdentity()
        {
            var h = New();
            var typed = h.Asset<TextAsset>(TextPath);
            var compatible = h.Asset<UnityEngine.Object>(TextPath);
            await Wait(() => typed.IsDone && compatible.IsDone, "Compatible concurrent requests");
            var cached = h.Asset<TextAsset>(TextPath);
            Check(cached.IsDone && typed.Result != null && typed.Result == compatible.Result && typed.Result == cached.Result,
                "Compatible typed entries expose the same real asset, with a synchronous same-type cache hit.");
            var firstLease = Get<AssetInfoCache.Lease<TextAsset>>(typed, "_lease");
            var secondLease = Get<AssetInfoCache.Lease<UnityEngine.Object>>(compatible, "_lease");
            var cachedLease = Get<AssetInfoCache.Lease<TextAsset>>(cached, "_lease");
            var entry = h.Entry("text.ab", TextPath);
            var objectEntry = h.Entry("text.ab", TextPath, typeof(UnityEngine.Object));
            Check(!ReferenceEquals(firstLease, cachedLease) && !ReferenceEquals(entry, objectEntry) &&
                ReferenceEquals(Get<AssetInfoCache>(firstLease, "_cache"), entry) &&
                ReferenceEquals(Get<AssetInfoCache>(cachedLease, "_cache"), entry) &&
                ReferenceEquals(Get<AssetInfoCache>(secondLease, "_cache"), objectEntry),
                "Independent leases bind to the exact entry for their requested type.");
            Check(h.AssetRefs("text.ab", TextPath) == 2 && Get<int>(objectEntry, "_refCount") == 1 && h.Refs("text.ab") == 2,
                "Two typed entries own two Bundle leases; same-type Handles share one entry.");
            typed.Dispose(); firstLease.Dispose(); typed.Dispose();
            Check(firstLease.Value == null && h.AssetRefs("text.ab", TextPath) == 1 && compatible.Result != null && cached.Result != null,
                "Repeated disposal returns exactly one reference to its owning typed entry.");
            compatible.Dispose();
            Check(Get<int>(objectEntry, "_refCount") == 0 && h.AssetRefs("text.ab", TextPath) == 1 && h.Refs("text.ab") == 1,
                "Releasing Object does not release the live TextAsset entry.");
            cached.Dispose();
            Check(h.AssetRefs("text.ab", TextPath) == 0 && entry.Stage == AssetCacheStage.Loaded &&
                objectEntry.Stage == AssetCacheStage.Loaded && h.Refs("text.ab") == 0 &&
                Get<AssetBundleCache.Lease>(entry, "_mainLease") == null &&
                Get<AssetBundleCache.Lease>(objectEntry, "_mainLease") == null,
                "Final releases preserve both typed indexes and return both Bundle leases.");
        }

        private static async UniTask DestroyedAssetLease()
        {
            var h = New(); var handle = h.Asset<TextAsset>(TextPath);
            await Wait(() => handle.IsDone, "Asset before deliberate destruction");
            Check(handle.Result != null && h.AssetRefs("text.ab", TextPath) == 1, "A successful Handle owns one live asset reference.");
            UnityEngine.Object.DestroyImmediate(handle.Result, true);
            Check(handle.Result == null && h.AssetRefs("text.ab", TextPath) == 1,
                "Native destruction invalidates the Unity object without returning the request reference.");
            handle.Dispose(); handle.Dispose();
            Check(h.AssetRefs("text.ab", TextPath) == 0 && h.Entry("text.ab", TextPath).Stage == AssetCacheStage.Loaded && h.Refs("text.ab") == 0,
                "Dispose returns the reference even though the Unity asset compares equal to null.");
            h.Assets.UnloadUnusedAssets();
            Check(!h.Has("text.ab"), "Destroyed asset no longer pins its entry or bundle after disposal and cleanup.");
        }

        private static async UniTask ExactAssetEntry()
        {
            var h = New(); var oldHandle = h.Asset<TextAsset>(TextPath);
            await Wait(() => oldHandle.IsDone, "Original entry load");
            var oldEntry = h.Entry("text.ab", TextPath);
            var entries = Get<Dictionary<(string, Type), AssetInfoCache>>(h.Cache("text.ab"), "_assetCacheMap");
            // Deliberate local harness mutation to distinguish instance-bound release from path lookup.
            entries.Remove((TextPath, typeof(TextAsset)));
            try
            {
                var replacement = h.Asset<TextAsset>(TextPath);
                await Wait(() => replacement.IsDone, "Replacement entry load");
                Check(!ReferenceEquals(oldEntry, h.Entry("text.ab", TextPath)) && replacement.Result != null &&
                    Get<int>(oldEntry, "_refCount") == 1 && h.AssetRefs("text.ab", TextPath) == 1,
                    "Same path now maps to a different live cache entry with its own request reference.");
                oldHandle.Dispose();
                Check(Get<int>(oldEntry, "_refCount") == 0 && oldEntry.Stage == AssetCacheStage.Loaded &&
                    h.AssetRefs("text.ab", TextPath) == 1 && h.Refs("text.ab") == 1 && replacement.Result != null,
                    "Old Handle returns only the original entry reference, leaving the replacement untouched.");
            }
            finally
            {
                oldHandle.Dispose();
                await oldEntry.DisposeAsync();
            }
            Check(h.Refs("text.ab") == 1, "Disposing the detached idle entry does not release the replacement Bundle lease.");
        }

        private static async UniTask UnloadedAssetLease()
        {
            var h = New(); var oldHandle = h.Asset<TextAsset>(TextPath);
            await Wait(() => oldHandle.IsDone, "Entry before forced unload");
            var oldEntry = h.Entry("text.ab", TextPath);
            await oldEntry.DisposeAsync();
            Get<Dictionary<(string, Type), AssetInfoCache>>(h.Cache("text.ab"), "_assetCacheMap").Remove((TextPath, typeof(TextAsset)));
            var replacement = h.Asset<TextAsset>(TextPath);
            await Wait(() => replacement.IsDone, "Replacement after old entry unload");
            oldHandle.Dispose(); oldHandle.Dispose();
            Check(oldEntry.Stage == AssetCacheStage.Unloaded && Get<int>(oldEntry, "_refCount") == 0,
                "Late old Lease disposal does not decrement or revive an unloaded owner.");
            Check(replacement.Result != null && h.AssetRefs("text.ab", TextPath) == 1 && h.Refs("text.ab") == 1,
                "Same-path replacement keeps both its request reference and entry-owned Bundle lease.");
        }

        private static async UniTask EditorAssetLease()
        {
            var setting = LiteRuntime.Setting.Asset;
            var previousDelay = setting.SimulateAsyncDelayInEditor;
            var previousMin = setting.AsyncDelayMinFrame;
            var previousMax = setting.AsyncDelayMaxFrame;
            try
            {
                foreach (var simulateDelay in new[] { false, true })
                {
                    setting.SimulateAsyncDelayInEditor = simulateDelay;
                    setting.AsyncDelayMinFrame = 2;
                    setting.AsyncDelayMaxFrame = 3;
                    var provider = new AssetEditorProvider();
                    Check(await provider.Initialize(), "Editor Provider initializes with simulated delay=" + simulateDelay);
                    var assets = new AssetSystem();
                    Set(assets, "_provider", provider);
                    using var live = assets.LoadAssetHandle<TextAsset>(EditorTextPath);
                    using var cts = new CancellationTokenSource();
                    using var canceled = assets.LoadAssetHandle<TextAsset>(EditorTextPath, ct: cts.Token);
                    using var disposed = assets.LoadAssetHandle<TextAsset>(EditorTextPath);
                    using var failed = assets.LoadAssetHandle<TextAsset>("Assets/StandaloneAssets/_session_validation/missing.txt");
                    try
                    {
                        cts.Cancel(); disposed.Dispose();
                        var tokenCanceled = false; var disposeCanceled = false;
                        try { await canceled; } catch (OperationCanceledException) { tokenCanceled = true; }
                        try { await disposed; } catch (OperationCanceledException) { disposeCanceled = true; }
                        await Wait(() => live.IsDone && canceled.IsDone && disposed.IsDone && failed.IsDone, "Editor delayed results");
                        var lease = Get<AssetInfoCache.Lease<TextAsset>>(live, "_lease");
                        Check(live.Result != null && lease != null && Get<AssetInfoCache>(lease, "_cache") == null,
                            "Editor success delivers an uncached Lease through the public Handle API.");
                        Check(tokenCanceled && disposeCanceled && canceled.Result == null && disposed.Result == null &&
                            Get<AssetInfoCache.Lease<TextAsset>>(canceled, "_lease") == null &&
                            Get<AssetInfoCache.Lease<TextAsset>>(disposed, "_lease") == null,
                            "Canceled and disposed Editor Handles release their late leases.");
                        Check(failed.Result == null && Get<AssetInfoCache.Lease<TextAsset>>(failed, "_lease") == null,
                            "Editor load failure has neither result nor Lease.");
                        var asset = live.Result; live.Dispose(); live.Dispose(); lease.Dispose();
                        Check(live.Result == null && lease.Value == null && asset != null,
                            "Editor Lease disposal clears ownership without destroying the AssetDatabase asset.");
                    }
                    finally
                    {
                        live.Dispose(); canceled.Dispose(); disposed.Dispose(); failed.Dispose();
                        await assets.DisposeAsync();
                    }
                }
            }
            finally
            {
                setting.SimulateAsyncDelayInEditor = previousDelay;
                setting.AsyncDelayMinFrame = previousMin;
                setting.AsyncDelayMaxFrame = previousMax;
            }
        }

        private static async UniTask RetentionDuringPause()
        {
            var h = New();
            var handle = h.Asset<TextAsset>(TextPath);
            await Wait(() => handle.IsDone, "Pause retention fixture load");
            Check(handle.Result != null, "A real asset is loaded before testing paused retention.");
            var oldScale = Time.timeScale;
            var oldRetainTime = LiteRuntime.Setting.Asset.BundleRetainTime;
            try
            {
                LiteRuntime.Setting.Asset.BundleRetainTime = 0.05f;
                handle.Dispose();
                var cache = h.Cache("text.ab");
                Time.timeScale = 0f;
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                var started = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - started < 0.2f)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                    h.Assets.Tick(Time.deltaTime);
                }
                _case.evidence.Add($"Paused elapsed={Time.realtimeSinceStartup - started:F3}s, deltaTime={Time.deltaTime}, unscaledDelta={LiteTime.UnscaledDeltaTime}, remaining={Get<float>(cache, "_retainTime")}, cached={h.Has("text.ab")}.");
                Check(!h.Has("text.ab"), "The 0.05-second retention deadline must expire after 0.2 real seconds while timeScale is zero.");
            }
            finally
            {
                Time.timeScale = oldScale;
                LiteRuntime.Setting.Asset.BundleRetainTime = oldRetainTime;
            }
        }

        private static async UniTask WaitWithoutLoaderTick(Func<bool> predicate, string operation)
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(operation);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }
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
            task.Task.GetAwaiter().OnCompleted(() =>
            {
                bundle = task.GetBundle();
                shutdown = h.Shutdown();
                waitingForDelivery = bundle != null && !shutdown.Status.IsCompleted();
            });
            try
            {
                await WaitWithoutLoaderTick(() => task.WaitForCleanupAsync().Status.IsCompleted(), "Local Bundle callback cleanup");
                Check(waitingForDelivery, "Shutdown from the successful Task continuation waits for the pending result callback and cleanup.");
                await shutdown;
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

        private static async UniTask FailedLocalBundleCleanup()
        {
            var callbacks = 0;
            var task = new LoadLocalBundleTask(Path.Combine(BundleFolder, "text.ab"), _ => callbacks++);
            task.Execute();
            var request = Get<AssetBundleCreateRequest>(task, "_bundleRequest");
            var error = new InvalidOperationException("Controlled local Bundle failure");
            try
            {
                task.Fail(error); task.Dispose(); task.Cancel();
                await WaitWithoutLoaderTick(() => request.isDone, "Failed local task native completion");
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                Check(request.assetBundle == null, "A failed and already-disposed task releases its late native Bundle without Loader Tick.");
                Check(task.State == TaskState.Failed && ReferenceEquals(task.Error, error) && task.Result == null
                    && callbacks == 1 && await task.Task == null, "Late native cleanup preserves the failure, null await result and one failure callback.");
            }
            finally
            {
                task.Cancel(); task.Dispose();
                if (request != null && request.isDone && request.assetBundle != null) request.assetBundle.Unload(true);
            }
        }

        private static async UniTask LoaderShutdownHandoff()
        {
            var textInfo = new BundleInfo(1, "text.ab", new[] { TextPath }, null);
            var deliveredInfo = new BundleInfo(2, "b.ab", new[] { MaterialPath }, null);
            var pendingInfo = new BundleInfo(3, "a.ab", new[] { PrefabPath }, null);
            var pack = new VersionPackInfo("validation", "windows", false, false, new[] { textInfo, deliveredInfo, pendingInfo });
            var locator = new Locator(true);
            var loader = new AssetBundleLoader(locator, pack, 3);
            AssetBundle earlierDelivered = null;
            AssetBundle delivered = null;
            var shutdowns = new List<UniTask>();
            try
            {
                var discardedCallbacks = 0;
                var deliveredCallbacks = 0;
                var deliveredAlive = true;
                AssetLoadResult<AssetBundle> deliveredResult = null;
                loader.LoadBundle(textInfo, AssetLoadPriority.Normal, _ => discardedCallbacks++);
                loader.LoadBundle(deliveredInfo, AssetLoadPriority.Normal, result =>
                {
                    deliveredCallbacks++;
                    deliveredResult = result;
                    shutdowns.Add(loader.DisposeAsync()); shutdowns.Add(loader.DisposeAsync());
                    deliveredAlive &= result.Succeeded && result.Error == null && result.Value != null && ReferenceEquals(result.Value, delivered);
                });
                loader.LoadBundle(deliveredInfo, AssetLoadPriority.Normal, result =>
                {
                    deliveredCallbacks++;
                    deliveredAlive &= ReferenceEquals(result, deliveredResult);
                    deliveredAlive &= result.Succeeded && result.Error == null && result.Value != null && ReferenceEquals(result.Value, delivered);
                });
                loader.LoadBundle(pendingInfo, AssetLoadPriority.Normal, _ => discardedCallbacks++);
                loader.Tick(0);
                var reentries = 0;
                locator.Manual["a.ab"].Task.GetAwaiter().OnCompleted(() => { reentries++; shutdowns.Add(loader.DisposeAsync()); });
                earlierDelivered = AssetBundle.LoadFromFile(Path.Combine(BundleFolder, "text.ab"));
                delivered = AssetBundle.LoadFromFile(Path.Combine(BundleFolder, "b.ab"));
                Check(earlierDelivered != null && delivered != null, "The handoff fixture contains two independent real Bundles.");
                locator.Manual["text.ab"].Finish(earlierDelivered);
                locator.Manual["b.ab"].Finish(delivered);
                locator.Manual["b.ab"].Dispose();
                loader.Tick(0);
                foreach (var shutdown in shutdowns) await shutdown;
                Check(deliveredCallbacks == 2 && deliveredAlive && delivered != null,
                    "Closing from the first callback preserves the delivered Bundle and finishes shared callback dispatch.");
                Check(earlierDelivered != null && discardedCallbacks == 1 && reentries == 1
                    && locator.Manual["a.ab"].State == TaskState.Cancelled,
                    "Shutdown preserves the earlier callback's Bundle, suppresses pending delivery and tolerates cancellation-continuation reentry.");
                Check(locator.Manual["text.ab"].State == TaskState.Succeeded
                    && locator.Manual["b.ab"].State == TaskState.Succeeded,
                    "Resource cleanup does not rewrite successful Task history.");
                var rejected = 0;
                loader.LoadBundle(textInfo, AssetLoadPriority.High, result => { if (!result.Succeeded && result.Value == null && result.Error.ResourceKey == textInfo.BundlePath && !string.IsNullOrWhiteSpace(result.Error.Message)) rejected++; });
                loader.RaiseBundlePriority(textInfo, AssetLoadPriority.High); loader.Tick(0); await loader.DisposeAsync();
                Check(rejected == 1 && locator.Started.Count == 3,
                    "The closed Loader rejects new requests without starting additional work.");
            }
            finally
            {
                await loader.DisposeAsync();
                if (earlierDelivered != null) earlierDelivered.Unload(true);
                if (delivered != null) delivered.Unload(true);
            }
            await UniTask.Yield();
        }

        private static async UniTask ShutdownWithPendingDependency()
        {
            var h = New(true);
            var handle = h.Asset<GameObject>(PrefabPath);
            h.Provider.Tick(0);
            var main = h.Finish("a.ab", true);
            var loader = Get<AssetBundleLoader>(h.Provider, "_bundleLoader");
            Check(main != null && !handle.IsDone && h.Refs("b.ab") == 1,
                "The main Bundle has arrived, but the Handle still waits for its protected dependency.");
            await h.Shutdown(); await loader.DisposeAsync(); await h.Provider.DisposeAsync();
            Check(main == null && handle.IsDone && handle.Result == null && h.Locater.Manual["b.ab"].State == TaskState.Cancelled,
                "Cache shutdown settles dependency callbacks and rolls back the already-delivered main Bundle.");
            await UniTask.Yield();
        }

        private static async UniTask RemoteBundleShutdown()
        {
            var info = new BundleInfo(1, "text.ab", new[] { TextPath }, null);
            var pack = new VersionPackInfo("validation", "windows", false, false, new[] { info });
            foreach (var closeBeforeCallback in new[] { false, true })
            {
                var locator = new FileRemoteLocator();
                var loader = new AssetBundleLoader(locator, pack, 1);
                AssetBundle bundle = null;
                try
                {
                    var callbacks = 0;
                    UniTask shutdown = default;
                    var waitingForDelivery = false;
                    loader.LoadBundle(info, AssetLoadPriority.Normal, result => { callbacks++; bundle = result.Value; }); loader.Tick(0);
                    if (closeBeforeCallback)
                    {
                        locator.Task.Task.GetAwaiter().OnCompleted(() =>
                        {
                            bundle = locator.Task.GetBundle();
                            shutdown = loader.DisposeAsync();
                            waitingForDelivery = bundle != null && !shutdown.Status.IsCompleted();
                        });
                    }
                    await WaitWithoutLoaderTick(() => locator.Task.WaitForCleanupAsync().Status.IsCompleted(), "Remote-file Bundle callback cleanup");
                    if (closeBeforeCallback)
                    {
                        await shutdown;
                        Check(waitingForDelivery && bundle == null && callbacks == 0,
                            "Shutdown from the Remote Task continuation waits until its late successful Bundle is released.");
                    }
                    else
                    {
                        Check(bundle != null && callbacks == 1,
                            "The Remote Task delivers its Bundle without another Loader Tick.");
                    }
                    await loader.DisposeAsync(); await loader.DisposeAsync();
                    Check(locator.Task.State == TaskState.Succeeded && (closeBeforeCallback ? bundle == null && callbacks == 0 : bundle != null && callbacks == 1),
                        "Shutdown preserves successful Task history and only releases results not handed to consumers.");
                }
                finally
                {
                    await loader.DisposeAsync();
                    if (bundle != null) bundle.Unload(true);
                }
            }
        }

        private static async UniTask RemoteHttpCancellation(bool finishDownload)
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
                if (finishDownload)
                {
                    server.ReleaseBody();
                    // Keep Unity's main-thread completion callback pending while its download worker finishes.
                    var deadline = System.Diagnostics.Stopwatch.StartNew();
                    while (!handler.isDownloadComplete && deadline.Elapsed.TotalSeconds < 10 && !server.Work.IsFaulted)
                    {
                        Thread.Sleep(1);
                    }
                    Check(handler.isDownloadComplete && locator.Task.State == TaskState.Running && locator.Task.Result == null,
                        "Download finishes before the Task publishes a result.");
                }
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

        private static async UniTask TaskResultViews()
        {
            var canceledCalls = 0;
            var canceledBundle = new LoadLocalBundleTask("unused.ab", _ => canceledCalls++);
            canceledBundle.Cancel(); canceledBundle.Dispose();
            Check(canceledBundle.GetBundle() == null && canceledBundle.Result == null && canceledCalls == 0,
                "Queued Bundle cancellation still ends with null and no completion callback.");
            var canceledAsset = new LoadAssetTask<TextAsset>(null, TextPath, _ => canceledCalls++);
            canceledAsset.Cancel(); canceledAsset.Dispose();
            Check(canceledAsset.GetAsset() == null && canceledAsset.Result == null && canceledCalls == 1,
                "Queued Asset cancellation still notifies once with null.");

            AssetBundle localBundle = null;
            AssetBundle remoteBundle = null;
            try
            {
                var events = new List<string>();
                var bundleCalls = 0;
                var bundleMatches = false;
                LoadLocalBundleTask local = null;
                local = new LoadLocalBundleTask(Path.Combine(BundleFolder, "text.ab"), bundle =>
                {
                    bundleCalls++; events.Add("callback"); localBundle = bundle;
                    bundleMatches = ReferenceEquals(bundle, local.Result) && ReferenceEquals(bundle, local.GetBundle())
                        && Get<Delegate>(local, "_callback") == null;
                    local.Cancel();
                });
                var bundleWait = local.Task.GetAwaiter();
                bundleWait.OnCompleted(() => events.Add("await"));
                LiteRuntime.Task.AddTask(local);
                await Wait(() => local.IsDone, "Local Bundle result view");
                local.Dispose();
                Check(local.State == TaskState.Succeeded && localBundle != null && bundleCalls == 1 && bundleMatches
                    && events.SequenceEqual(new[] { "await", "callback" }), "Local Bundle keeps success notification order and one result identity.");
                Check(ReferenceEquals(local.GetBundle(), local.Result) && ReferenceEquals(bundleWait.GetResult(), localBundle),
                    "Bundle getter and await result survive wrapper cleanup without transferring new ownership.");

                events.Clear();
                var assetCalls = 0;
                var assetMatches = false;
                TextAsset loadedAsset = null;
                LoadAssetTask<TextAsset> assetTask = null;
                assetTask = new LoadAssetTask<TextAsset>(localBundle, AssetPath.Resolve(TextPath), completed =>
                {
                    var asset = completed.Result as TextAsset;
                    assetCalls++; events.Add("callback"); loadedAsset = asset;
                    assetMatches = ReferenceEquals(asset, assetTask.Result) && ReferenceEquals(asset, assetTask.GetAsset())
                        && Get<Delegate>(assetTask, "_callback") == null;
                    assetTask.Cancel();
                    throw new InvalidOperationException("Injected validation result observer");
                });
                var assetWait = assetTask.Task.GetAwaiter();
                assetWait.OnCompleted(() => events.Add("await"));
                LiteRuntime.Task.AddTask(assetTask);
                await Wait(() => assetTask.IsDone, "Asset result view");
                assetTask.Dispose(); assetTask.Dispose();
                Check(assetTask.State == TaskState.Succeeded && assetTask.Error == null && loadedAsset != null
                    && assetCalls == 1 && assetMatches && events.SequenceEqual(new[] { "await", "callback" }),
                    "Asset callback reentry and observer exceptions cannot replace success or change its order.");
                Check(ReferenceEquals(assetTask.GetAsset(), loadedAsset) && ReferenceEquals(assetTask.Result, loadedAsset)
                    && ReferenceEquals(assetWait.GetResult(), loadedAsset), "GetAsset keeps the same committed result after cleanup.");

                localBundle.Unload(true); localBundle = null;
                events.Clear();
                var remoteCalls = 0;
                var remoteMatches = false;
                LoadRemoteBundleTask remote = null;
                remote = new LoadRemoteBundleTask(new Uri(Path.Combine(BundleFolder, "text.ab")).AbsoluteUri, null, bundle =>
                {
                    remoteCalls++; events.Add("callback"); remoteBundle = bundle;
                    remoteMatches = ReferenceEquals(bundle, remote.Result) && ReferenceEquals(bundle, remote.GetBundle())
                        && Get<Delegate>(remote, "_callback") == null;
                    if (remoteCalls == 1)
                    {
                        var request = typeof(UnityDownloadBaseTask).GetField("_request", Private).GetValue(remote);
                        typeof(LoadRemoteBundleTask).GetMethod("OnSuccess", Private).Invoke(remote, new[] { request });
                    }
                });
                var remoteWait = remote.Task.GetAwaiter();
                remoteWait.OnCompleted(() => events.Add("await"));
                LiteRuntime.Task.AddTask(remote);
                await Wait(() => remote.IsDone, "Remote adapter file Bundle result view");
                remote.Dispose();
                Check(remote.State == TaskState.Succeeded && remoteBundle != null && remoteCalls == 1 && remoteMatches
                    && events.SequenceEqual(new[] { "await", "callback" }), "Repeated native success cannot deliver the remote Bundle twice.");
                Check(ReferenceEquals(remote.GetBundle(), remote.Result) && ReferenceEquals(remoteWait.GetResult(), remoteBundle),
                    "Remote Bundle getter reads the committed result after request cleanup.");
            }
            finally
            {
                if (localBundle != null) localBundle.Unload(true);
                if (remoteBundle != null) remoteBundle.Unload(true);
            }
        }
    }
}
