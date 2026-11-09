using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal static partial class AssetLifecycleValidationRunner
    {
        private static async UniTask SimulateMode()
        {
            New();
            var setting = LiteRuntime.Setting.Asset;
            var oldLocater = setting.BundleMode;
            var oldLimit = setting.ConcurrencyLimit;
            var oldRetain = setting.BundleRetainTime;
            var handles = new List<IDisposable>();
            var gates = new List<GateTask>();
            AssetSystem assets = null;
            T Keep<T>(T handle) where T : IDisposable { handles.Add(handle); return handle; }
            try
            {
                setting.BundleMode = BundleLoadMode.Simulate;
                setting.ConcurrencyLimit = 1;
                assets = new AssetSystem();
                var provider = Get<IAssetManager>(assets, "_manager") as AssetManager;
                Check(provider != null, "Simulate selects the existing Bundle Provider through normal AssetSystem construction.");
                UniTask Pump(Func<bool> done, string label) => Wait(() => { provider.Tick(0); return done(); }, label);

                var group = RuntimeGroup(LiteConst.TaskGroupId.Asset);
                gates = FillGroup(group.Id, group.ConcurrencyLimit.Value);
                await Wait(() => gates.All(task => task.IsExecuted), "Simulate version admission gate");
                // The running fixture Runtime already owns the Asset group; initialize the selected Provider directly.
                var initialization = provider.Initialize((IBundleProvider)typeof(AssetSystem).GetMethod("CreateBundleProvider", Private).Invoke(assets, new object[] { true }));
                var versionTask = TaskSnapshot().OfType<LoadSimulateVersionPackTask>().Single();
                Check(versionTask.State == TaskState.Pending && !initialization.Status.IsCompleted(),
                    "Manifest collection is submitted to the Asset group and waits for admission.");
                await ReleaseGates(gates); gates.Clear();
                Check(await initialization, "Simulate initializes from project collection without a built version file.");
                var pack = Get<VersionPackInfo>(provider, "_packInfo");
                Check(!pack.HashMode && !pack.FlatMode && assets.GetVersion() == AppUtils.GetVersion() && assets.HasAsset(EditorTextPath) &&
                    !assets.HasAsset("Assets/LiteQuark/Runtime/Asset/AssetSystem.cs"),
                    "Simulate uses initialized logical manifest keys and rejects existing project files outside collection.");

                var first = Keep(assets.LoadAssetHandle<TextAsset>(EditorTextPath));
                var second = Keep(assets.LoadAssetHandle<TextAsset>(EditorTextPath.ToUpperInvariant()));
                provider.Tick(0);
                var bundleTask = TaskSnapshot().OfType<LoadSimulateBundleTask>().Single();
                await Wait(() => bundleTask.State == TaskState.Running, "Simulate Bundle execution");
                Check(!bundleTask.IsDone && !first.IsDone, "Simulate Bundle execution spans a Task tick before result delivery.");
                await Pump(() => first.IsDone && second.IsDone, "Shared simulated assets");
                var mainInfo = pack.GetBundleInfoFromAssetPath(EditorTextPath);
                var cache = provider.GetOrCreateBundleCache(mainInfo.BundlePath);
                Check(first.Result != null && ReferenceEquals(first.Result, second.Result) && cache.Bundle is SimulateBundleResource &&
                    Get<int>(cache, "_refCount") == 1 && cache.Bundle.GetRuntimeMemorySize() == 0,
                    "Aliases share the existing typed asset cache and one Bundle lease; simulated memory statistics are not native measurements.");

                const string prefix = "Assets/StandaloneAssets/_session_validation/Simulate/";
                gates = FillGroup(group.Id, group.ConcurrencyLimit.Value);
                await Wait(() => gates.All(task => task.IsExecuted), "Simulate Bundle admission gate");
                var low = Keep(assets.LoadAssetHandle<TextAsset>(prefix + "Low/Value.txt", AssetLoadPriority.Low));
                var normal = Keep(assets.LoadAssetHandle<TextAsset>(prefix + "Normal/Value.txt"));
                var high = Keep(assets.LoadAssetHandle<TextAsset>(prefix + "High/Value.txt", AssetLoadPriority.High));
                provider.Tick(0);
                bundleTask = TaskSnapshot().OfType<LoadSimulateBundleTask>().Single();
                Check(bundleTask.DebugName.Contains(pack.GetBundleInfoFromAssetPath(prefix + "High/Value.txt").BundlePath) && !bundleTask.IsExecuted &&
                    Get<System.Collections.IList>(Get<AssetBundleLoader>(provider, "_bundleLoader"), "_runningTasks").Count == 1,
                    "Loader submits High first, respects its one-slot limit and still waits for Asset-group admission.");
                await ReleaseGates(gates); gates.Clear();
                await Pump(() => low.IsDone && normal.IsDone && high.IsDone, "Simulated priority loads");
                Check(low.Result.text == "Low" && normal.Result.text == "Normal" && high.Result.text == "High",
                    "Simulated resources resolve normalized keys back to mixed-case source paths.");
                var lowInfo = pack.GetBundleInfoFromAssetPath(prefix + "Low/Value.txt");
                var lowResource = provider.GetOrCreateBundleCache(lowInfo.BundlePath).Bundle;
                var projectAsset = low.Result;
                setting.BundleRetainTime = 2;
                low.Dispose(); normal.Dispose(); high.Dispose();
                provider.Tick(1);
                Check(provider.TryGetBundleCache(lowInfo.BundlePath, out var retained) && retained.IsRetained,
                    "Simulated zero-reference Bundles enter the existing retention period.");
                provider.Tick(2);
                var disposed = false;
                try { lowResource.GetAllScenePaths(); } catch (ObjectDisposedException) { disposed = true; }
                Check(!provider.TryGetBundleCache(lowInfo.BundlePath, out _) && disposed && projectAsset != null,
                    "Retention disposes the simulated container without destroying the shared AssetDatabase object.");

                var missing = Keep(assets.LoadAssetHandle<TextAsset>(prefix + "Missing.txt"));
                var wrongType = Keep(assets.LoadAssetHandle<Texture2D>(EditorTextPath));
                await Pump(() => wrongType.IsDone, "Simulate type failure");
                Check(missing.State == AssetHandleState.Failed && wrongType.State == AssetHandleState.Failed && wrongType.Error != null,
                    "Uncollected resources and incompatible requested types fail through existing Handle semantics.");
                var cancelled = Keep(assets.LoadAssetHandle<TextAsset>(prefix + "High/Value.txt"));
                var surviving = Keep(assets.LoadAssetHandle<TextAsset>(prefix + "High/Value.txt"));
                cancelled.Dispose();
                await Pump(() => surviving.IsDone, "Simulate shared cancellation");
                Check(cancelled.State == AssetHandleState.Cancelled && cancelled.Result == null && surviving.Result != null,
                    "Canceling one Handle does not cancel shared simulated loading or deliver a late result to it.");

                var scene = Keep(assets.LoadSceneHandle(EditorScenePath, new LoadSceneParameters(LoadSceneMode.Additive)));
                var duplicate = Keep(assets.LoadSceneHandle(EditorScenePath, new LoadSceneParameters(LoadSceneMode.Additive)));
                var sceneInfo = pack.GetBundleInfoFromAssetPath(EditorScenePath);
                var sceneCache = provider.GetOrCreateBundleCache(sceneInfo.BundlePath);
                var entries = Get<List<SceneInfoCache>>(sceneCache, "_sceneCacheMap");
                Check(duplicate.State == AssetHandleState.Failed && Get<BaseTask>(entries.Last(), "_loadSceneTask") is LoadEditorSceneTask,
                    "Simulate uses the shared scene cache and Editor scene task, including same-path concurrent rejection.");
                await Pump(() => scene.IsDone, "Simulate scene");
                var actualScene = scene.Result.Value;
                Check(actualScene.path == EditorScenePath && sceneInfo.DependencyList.Length > 0 &&
                    sceneInfo.DependencyList.All(path => provider.TryGetBundleCache(path, out var dependency) && Get<int>(dependency, "_refCount") > 0),
                    "Collected scene dependencies are held by the existing Bundle leases.");
                var anotherScene = Keep(assets.LoadSceneHandle(EditorScenePath, new LoadSceneParameters(LoadSceneMode.Additive)));
                await Pump(() => anotherScene.IsDone, "Second simulated scene instance");
                var otherActualScene = anotherScene.Result.Value;
                scene.Dispose();
                await Pump(() => !actualScene.IsValid() || !actualScene.isLoaded, "First simulated scene unload");
                Check(otherActualScene.isLoaded && otherActualScene.handle != actualScene.handle,
                    "Additive simulated scenes retain separate instance ownership.");

                var cancelledScene = Keep(assets.LoadSceneHandle(EditorSecondScenePath, new LoadSceneParameters(LoadSceneMode.Additive)));
                cancelledScene.Dispose();
                await Pump(() => Get<HashSet<string>>(provider, "_loadingSceneSet").Count == 0 &&
                    !Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(value => value.path == EditorSecondScenePath),
                    "Cancelled simulated scene cleanup");
                Check(cancelledScene.State == AssetHandleState.Cancelled && cancelledScene.Result == null,
                    "A cancelled scene Handle does not retain its late Editor scene.");

                var pending = Keep(assets.LoadAssetHandle<TextAsset>(prefix + "Low/Value.txt"));
                provider.Tick(0);
                bundleTask = TaskSnapshot().OfType<LoadSimulateBundleTask>().Single();
                await Wait(() => bundleTask.State == TaskState.Running, "Simulate shutdown during Bundle work");
                var pendingResource = Get<IBundleResource>(bundleTask, "_resource");
                await assets.DisposeAsync();
                disposed = false;
                try { pendingResource.GetAllScenePaths(); } catch (ObjectDisposedException) { disposed = true; }
                Check(disposed && bundleTask.State == TaskState.Cancelled && (await bundleTask.Task) == null &&
                    ((IResourceTaskCleanup)bundleTask).WaitForCleanupAsync().Status == UniTaskStatus.Succeeded && pending.IsDone && pending.Result == null &&
                    (!otherActualScene.IsValid() || !otherActualScene.isLoaded) && projectAsset != null,
                    "Closing cancels managed Bundle work, ends pending Handles and unloads actual scenes without destroying project assets.");
                Check(assets.LoadAssetHandle<TextAsset>(EditorTextPath).State == AssetHandleState.Failed,
                    "A closed simulated AssetSystem rejects new requests.");

                assets = new AssetSystem();
                provider = (AssetManager)Get<IAssetManager>(assets, "_manager");
                var backend = (IBundleProvider)typeof(AssetSystem).GetMethod("CreateBundleProvider", Private).Invoke(assets, new object[] { true });
                setting.BundleMode = BundleLoadMode.BuiltIn;
                Check(await provider.Initialize(backend) && !ReferenceEquals(pack, Get<VersionPackInfo>(provider, "_packInfo")),
                    "A new Simulate Provider collects a separate manifest session.");
                Check(ReferenceEquals(Get<IBundleProvider>(Get<AssetBundleLoader>(provider, "_bundleLoader"), "_bundleProvider"), backend),
                    "The manager uses its injected backend without reselecting from the global mode.");
                await assets.DisposeAsync();
                assets = new AssetSystem();
                Check(Get<IAssetManager>(assets, "_manager") is AssetManager,
                    "All modes use the same Bundle Provider.");
            }
            finally
            {
                foreach (var gate in gates) gate.Cancel();
                foreach (var handle in handles) handle.Dispose();
                try { if (assets != null) await assets.DisposeAsync(); }
                finally
                {
                    setting.BundleMode = oldLocater;
                    setting.ConcurrencyLimit = oldLimit;
                    setting.BundleRetainTime = oldRetain;
                }
            }
        }

        private static async UniTask RuntimeRestartPendingUI()
        {
            var runtime = LiteRuntime.Instance;
            var uiSystemType = Type.GetType("LiteQuark.Runtime.UISystem, LiteQuark.Runtime.Addons", true);
            ISystem UISystem() => (ISystem)typeof(LiteRuntime).GetMethod(nameof(LiteRuntime.Get)).MakeGenericMethod(uiSystemType).Invoke(null, null);
            var opened = new List<object>();
            var updates = 0;
            var closes = 0;
            var fixture = new RestartFixture(ui =>
            {
                opened.Add(ui);
                ui.GetType().GetField("OnUpdateCallback").SetValue(ui, (Action<object>)(_ => updates++));
                ui.GetType().GetField("OnCloseCallback").SetValue(ui, (Action<object>)(_ => closes++));
            });
            var probeType = fixture.ProbeType;
            var content = fixture.ContentFolder;
            try
            {
                Directory.CreateDirectory(content);
                Check((File.GetAttributes(fixture.Scratch) & FileAttributes.ReparsePoint) == 0,
                    "Restart content uses a new ordinary directory in the owned Scratch root.");
                foreach (var bundle in new[] { "a.ab", "b.ab" }) File.Copy(Path.Combine(BundleFolder, bundle), Path.Combine(content, bundle));
                var pack = new VersionPackInfo(AppUtils.GetVersion(), AppUtils.GetCurrentPlatformName(), false, false, new[]
                {
                    new BundleInfo(1, "b.ab", new[] { MaterialPath }, null),
                    new BundleInfo(2, "a.ab", new[] { PrefabPath }, new[] { "b.ab" })
                });
                File.WriteAllBytes(Path.Combine(content, AppUtils.GetVersionFileName()), pack.ToBinaryData());
                var setting = new LiteSetting();
                setting.Asset.BundleMode = BundleLoadMode.Remote;
                setting.Asset.BundleRemoteUri = new Uri(fixture.Scratch + Path.DirectorySeparatorChar).AbsoluteUri;
                setting.Asset.DisableUnityWebCache = true;
                setting.Asset.TaskConcurrencyLimit = 1;
                setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = typeof(RestartSystemProbe).AssemblyQualifiedName });
                setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = uiSystemType.AssemblyQualifiedName });
                setting.LogicList.Add(new LiteTypeEntryData<ILogic> { AssemblyQualifiedName = typeof(RestartLogicProbe).AssemblyQualifiedName });
                var uiSetting = (ISystemSetting)Activator.CreateInstance(Type.GetType("LiteQuark.Runtime.UISystemSetting, LiteQuark.Runtime.Addons", true));
                uiSetting.GetType().GetField("UICamera").SetValue(uiSetting, fixture.Camera.GetComponent<Camera>());
                setting.SystemSettings.Add(uiSetting);

                await runtime.Shutdown();
                _restartFixture = fixture;
                runtime.Startup(runtime.Launcher, setting);
                await WaitWithoutLoaderTick(() => runtime.State == LiteRuntime.RuntimeState.Running, "Restart fixture startup");
                var oldSystems = Get<List<ISystem>>(SystemCenter.Instance, "_ownedSystems").ToArray();
                var oldUI = UISystem();
                var oldRoot = GetUICanvasRoot(oldUI);
                fixture.OldAsset = LiteRuntime.Asset;
                fixture.OldTask = LiteRuntime.Task;
                var provider = Get<AssetManager>(fixture.OldAsset, "_manager");

                // Warm only the Object-typed cache: GameObject extraction must still enter the real Asset task group.
                using (var warmup = LiteRuntime.Asset.LoadAssetHandle<UnityEngine.Object>(PrefabPath))
                {
                    await WaitWithoutLoaderTick(() => warmup.IsDone, "Restart prefab Bundle warmup");
                    await warmup;
                    Check(warmup.Result is GameObject && oldSystems.Length == 9 && fixture.Logics.Count == 1,
                        "Runtime config initializes all seven built-ins, the UI and the probes, then loads real dependent Bundles.");
                }
                var oldBundles = new[] { provider.GetOrCreateBundleCache("a.ab"), provider.GetOrCreateBundleCache("b.ab") };
                fixture.Gates = FillGroup(LiteConst.TaskGroupId.Asset, 1);
                await WaitWithoutLoaderTick(() => fixture.Gates[0].IsExecuted, "Restart Asset group gate");
                var pending = OpenUIProbe(oldUI, probeType).Preserve();
                var entry = Get<Dictionary<(string, Type), AssetInfoCache>>(oldBundles[0], "_assetCacheMap")[(PrefabPath, typeof(GameObject))];
                var assetTask = Get<BaseTask>(entry, "_loadAssetTask");
                Check(pending.Status == UniTaskStatus.Pending && assetTask.State == TaskState.Pending && opened.Count == 0,
                    "The old OpenUI is held at real prefab extraction, not by a timing-dependent frame delay.");

                fixture.RequestRestart = true;
                await WaitWithoutLoaderTick(() => fixture.Events.Contains("system 1 disposing"), "Business Restart reaches asynchronous system disposal");
                Check(fixture.Events.SequenceEqual(new[] { "system 1 constructed", "logic 1 initialized", "logic 1 restart requested", "logic 1 disposed", "system 1 disposing" }) &&
                    runtime.State == LiteRuntime.RuntimeState.ShuttingDown && pending.Status == UniTaskStatus.Canceled && assetTask.State == TaskState.Pending,
                    "A real Logic.Tick calls public Restart; Runtime releases Logic and cancels UI before waiting for the next system.");
                var cancelled = false;
                try { await pending; } catch (OperationCanceledException) { cancelled = true; }
                Check(cancelled && ReferenceEquals(GetUICanvasRoot(oldUI), null) && Get<bool>(oldUI, "_isClosing"),
                    "The old OpenUI propagates cancellation and its registered UISystem clears Canvas ownership.");

                fixture.Gates[0].Cancel();
                await WaitWithoutLoaderTick(() => entry.IsLoaded, "Late real prefab extraction after UI cancellation");
                await UniTask.NextFrame();
                Check(assetTask.State == TaskState.Succeeded && entry.Asset is GameObject && Get<int>(entry, "_refCount") == 0 && opened.Count == 0,
                    "The cancelled UI rejects an actual late successful prefab result and returns its lease without calling OnOpen.");
                Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown && fixture.Systems.Count == 1 && fixture.Logics.Count == 1 && oldRoot == null,
                    "Old Canvas destruction completes while the outstanding system disposal still prevents any new round.");

                fixture.DisposeGate.TrySetResult();
                await WaitWithoutLoaderTick(() => runtime.State == LiteRuntime.RuntimeState.ShutdownFailed ||
                    fixture.Logics.Count == 2 && runtime.State == LiteRuntime.RuntimeState.Running, "New Runtime generation Running");
                fixture.Failure?.Throw();
                var newUI = UISystem();
                var newRoot = GetUICanvasRoot(newUI);
                var newSystems = Get<List<ISystem>>(SystemCenter.Instance, "_ownedSystems");
                Check(fixture.PreviousRoundReleased && fixture.Events.SequenceEqual(new[] { "system 1 constructed", "logic 1 initialized", "logic 1 restart requested",
                    "logic 1 disposed", "system 1 disposing", "system 1 disposed", "system 2 constructed", "logic 2 initialized" }) &&
                    fixture.Systems[0].Disposals == 1 && fixture.Logics[0].Disposals == 1 && fixture.Logics[0].TicksAfterDispose == 0,
                    "Old Logic and system disposal finish exactly once, including Asset and Task cleanup, before the next probe is constructed.");
                Check(newSystems.Count == oldSystems.Length && newSystems.All(system => !oldSystems.Contains(system)) &&
                    !ReferenceEquals(LiteRuntime.Asset, fixture.OldAsset) && !ReferenceEquals(LiteRuntime.Task, fixture.OldTask) && !ReferenceEquals(newUI, oldUI) &&
                    oldBundles.All(bundle => bundle.Bundle == null) && Get<Dictionary<string, AssetBundleCache>>(provider, "_bundleCacheMap").Count == 0,
                    "Every registered System is replaced, and the old Provider unloads both the prefab Bundle and its dependency.");

                var request = OpenUIProbe(newUI, probeType).Preserve();
                await WaitWithoutLoaderTick(() => request.Status.IsCompleted(), "New UISystem opens the same prefab");
                var ui = await request;
                var go = ui == null ? null : (GameObject)probeType.GetProperty("Go").GetValue(ui);
                Check(ui != null && opened.Count == 1 && ReferenceEquals(opened[0], ui) && ReferenceEquals(probeType.GetProperty("System").GetValue(ui), newUI) &&
                    go != null && go.transform.IsChildOf(newRoot) && go.GetComponent<Renderer>().sharedMaterial != null,
                    "The new round reloads the same prefab and dependency, calls only the new OnOpen, and parents it under the new Canvas.");
                await WaitWithoutLoaderTick(() => updates > 0, "New UI participates in Runtime Tick");
                newUI.GetType().GetMethods().Single(method => method.Name == "CloseUI" && !method.IsGenericMethod).Invoke(newUI, new[] { ui });
                await WaitWithoutLoaderTick(() => go == null, "New UI deferred close destruction");
                var newProvider = Get<AssetManager>(LiteRuntime.Asset, "_manager");
                var newEntry = Get<Dictionary<(string, Type), AssetInfoCache>>(newProvider.GetOrCreateBundleCache("a.ab"), "_assetCacheMap")[(PrefabPath, typeof(GameObject))];
                Check(closes == 1 && probeType.GetProperty("State").GetValue(ui).ToString() == "Closed" &&
                    (GameObject)probeType.GetProperty("Go").GetValue(ui) == null && Get<int>(newEntry, "_refCount") == 0,
                    "The new UISystem updates and closes the UI through normal Runtime-driven queues, returning its instance and prefab lease.");
                var roots = UnityEngine.Object.FindObjectsOfType<RectTransform>(true).Where(root => root.parent == null && root.name == "Canvas").ToArray();
                Check(oldRoot == null && roots.Length == 1 && roots[0] == newRoot && opened.Count == 1 && fixture.Logics[0].TicksAfterDispose == 0,
                    "After delayed destruction, only the new round's Canvas root remains and the old round stays inactive.");
            }
            catch (Exception ex) { fixture.CaptureFailure(ex); }
            finally { await FinishRestartFixture(fixture); }
        }

        private static async UniTask FinishRestartFixture(RestartFixture fixture)
        {
            var runtime = LiteRuntime.Instance;
            // Keep cleanup local to this fixture, but attempt independent steps even if shutdown or an earlier step fails.
            void Clean(Action action)
            {
                try { action(); }
                catch (Exception ex) { fixture.Errors.Add(ex); }
            }

            if (fixture.Gates != null) foreach (var gate in fixture.Gates) Clean(gate.Cancel);
            Clean(() => fixture.DisposeGate.TrySetResult());
            try
            {
                if (runtime.State == LiteRuntime.RuntimeState.ShuttingDown)
                    await WaitWithoutLoaderTick(() => runtime.State != LiteRuntime.RuntimeState.ShuttingDown, "Restart fixture leaves shutdown");
                if (runtime.State == LiteRuntime.RuntimeState.ShutdownFailed)
                    throw new InvalidOperationException("Restart fixture Runtime remains ShutdownFailed; shutdown retry and baseline startup are skipped.");
                if (runtime.State != LiteRuntime.RuntimeState.Idle)
                {
                    fixture.CleanupShutdownCalls++;
                    await runtime.Shutdown();
                }
                await WaitWithoutLoaderTick(() => !UnityEngine.Object.FindObjectsOfType<RectTransform>(true).Any(root => root.parent == null && root.name == "Canvas"),
                    "Restart fixture Canvas cleanup");
                Check(fixture.Systems.All(system => system.Disposals == 1) && fixture.Logics.All(logic => logic.Disposals == 1 && logic.TicksAfterDispose == 0),
                    "Fixture teardown disposes both generations exactly once, removes their Canvas roots and never ticks a released Logic.");
            }
            catch (Exception ex) { fixture.Errors.Add(ex); }

            UniTaskScheduler.UnobservedTaskException -= fixture.OnUnobserved;
            _restartFixture = null;
            Clean(() => UnityEngine.Object.DestroyImmediate(fixture.Camera));
            Clean(() => fixture.ProbeType.GetField("OnOpenCallback").SetValue(null, null));
            foreach (var file in new[] { "a.ab", "b.ab", AppUtils.GetVersionFileName() })
                Clean(() => File.Delete(Path.Combine(fixture.ContentFolder, file)));
            foreach (var folder in new[] { fixture.ContentFolder, fixture.PlatformFolder, fixture.Scratch })
                Clean(() => { if (Directory.Exists(folder)) Directory.Delete(folder); });
            _case.evidence.Add("Restart lifecycle: " + string.Join(" -> ", fixture.Events));

            if (runtime.State == LiteRuntime.RuntimeState.Idle)
            {
                try
                {
                    fixture.BaselineStartups++;
                    runtime.Startup(runtime.Launcher, fixture.OriginalSetting);
                    await WaitWithoutLoaderTick(() => runtime.State == LiteRuntime.RuntimeState.Running, "Restore baseline Runtime after restart fixture");
                }
                catch (Exception ex) { fixture.Errors.Add(ex); }
            }

            if (fixture.Failure != null)
            {
                if (fixture.Errors.Count == 0) fixture.Failure.Throw();
                throw new AggregateException("Restart fixture failed; additional errors include cleanup failures.",
                    new[] { fixture.Failure.SourceException }.Concat(fixture.Errors));
            }
            if (fixture.Errors.Count == 1) ExceptionDispatchInfo.Capture(fixture.Errors[0]).Throw();
            if (fixture.Errors.Count > 1) throw new AggregateException("Restart fixture cleanup failed.", fixture.Errors);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ThrowRestartBodyFailure(Exception error) => throw error;

        private static async UniTask RuntimeRestartFailureCleanup()
        {
            var runtime = LiteRuntime.Instance;
            var launcher = runtime.Launcher;
            var fixture = new RestartFixture();
            var bodyError = new InvalidOperationException("Injected restart fixture body failure");
            var disposeFailure = new RestartDisposeFailure();

            bool Released(RestartFixture value)
            {
                var observers = (Delegate)typeof(UniTaskScheduler).GetField("UnobservedTaskException",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                return _restartFixture == null && value.Camera == null && !Directory.Exists(value.Scratch) &&
                    value.ProbeType.GetField("OnOpenCallback").GetValue(null) == null &&
                    !(observers?.GetInvocationList().Any(callback => ReferenceEquals(callback.Target, value)) ?? false);
            }

            async UniTask ExerciseFailure()
            {
                try
                {
                    Directory.CreateDirectory(fixture.ContentFolder);
                    File.WriteAllText(Path.Combine(fixture.ContentFolder, AppUtils.GetVersionFileName()), "Owned restart cleanup fixture");
                    await runtime.Shutdown();
                    _restartFixture = fixture;
                    var setting = new LiteSetting();
                    setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = typeof(RestartSystemProbe).AssemblyQualifiedName });
                    setting.LogicList.Add(new LiteTypeEntryData<ILogic> { AssemblyQualifiedName = typeof(RestartLogicProbe).AssemblyQualifiedName });
                    runtime.Startup(launcher, setting);
                    await WaitWithoutLoaderTick(() => runtime.State == LiteRuntime.RuntimeState.Running, "Failure fixture startup");

                    // Inject at the closing tail so real systems and native resources are already released on failure.
                    // The Runtime must retain ShutdownFailed; this is not a recovery or retry implementation.
                    Get<CancellationTokenSource>(runtime, "_startupCts").Dispose();
                    Set(runtime, "_startupCts", disposeFailure);
                    fixture.Gates = FillGroup(LiteConst.TaskGroupId.Asset, 1);
                    await WaitWithoutLoaderTick(() => fixture.Gates[0].IsExecuted, "Failure fixture Asset gate");
                    fixture.RequestRestart = true;
                    await WaitWithoutLoaderTick(() => fixture.Events.Contains("system 1 disposing"), "Failure fixture Restart disposal");
                    Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown && !fixture.DisposeGate.Task.Status.IsCompleted() && !fixture.Gates[0].IsDone,
                        "The failure scenario enters the real public Restart path with both fixture gates still held.");
                    ThrowRestartBodyFailure(bodyError);
                }
                catch (Exception ex) { fixture.CaptureFailure(ex); }
                finally { await FinishRestartFixture(fixture); }
            }

            try
            {
                Exception reported = null;
                try { await ExerciseFailure(); }
                catch (Exception ex) { reported = ex; }
                var aggregate = reported as AggregateException;
                Check(aggregate != null && aggregate.InnerExceptions.Count == 3 && ReferenceEquals(aggregate.InnerExceptions[0], bodyError) &&
                    ReferenceEquals(aggregate.InnerExceptions[1], disposeFailure.Error) && aggregate.InnerExceptions[2].Message.Contains("Runtime remains ShutdownFailed"),
                    "The final exception retains the body failure first, followed by the actual Dispose failure and the explicit unrecovered-state cleanup error.");
                Check(bodyError.StackTrace.Contains(nameof(ThrowRestartBodyFailure)) && disposeFailure.Error.StackTrace.Contains(nameof(RestartDisposeFailure)) &&
                    !reported.ToString().Contains("Cannot shutdown runtime in state") && !reported.ToString().Contains("Cannot startup runtime in state"),
                    "Original body and Dispose stacks survive cleanup; neither is replaced by a second Shutdown or non-Idle Startup exception.");
                _case.evidence.Add("Expected failure report (caught by this regression): " + reported);
                Check(runtime.State == LiteRuntime.RuntimeState.ShutdownFailed && disposeFailure.Disposals == 1 &&
                    fixture.CleanupShutdownCalls == 0 && fixture.BaselineStartups == 0 && fixture.Systems.Count == 1 && fixture.Logics.Count == 1 &&
                    fixture.Systems[0].Disposals == 1 && fixture.Logics[0].Disposals == 1 && Get<List<ISystem>>(SystemCenter.Instance, "_ownedSystems").Count == 0,
                    "Tail Dispose failure leaves the real Runtime terminal, with no retry, baseline startup, new generation or remaining owned System.");
                Check(fixture.DisposeGate.Task.Status == UniTaskStatus.Succeeded && fixture.Gates.All(gate => gate.IsDone && gate.Disposed) && Released(fixture),
                    "Cleanup releases both gates and removes the camera, callback, exception subscription, static fixture and Scratch even after shutdown fails.");

                // Exercise the cleanup-only failure branch in the same terminal process, without resetting Runtime state.
                var cleanupOnly = new RestartFixture();
                Exception cleanupError = null;
                try { Directory.CreateDirectory(cleanupOnly.ContentFolder); }
                finally
                {
                    try { await FinishRestartFixture(cleanupOnly); }
                    catch (Exception ex) { cleanupError = ex; }
                }
                Check(cleanupOnly.Failure == null && cleanupError is InvalidOperationException && cleanupError.Message.Contains("Runtime remains ShutdownFailed") &&
                    cleanupOnly.Errors.Count == 1 && cleanupOnly.CleanupShutdownCalls == 0 && cleanupOnly.BaselineStartups == 0 && disposeFailure.Disposals == 1,
                    "Without a body failure, a genuine cleanup failure still propagates and an already failed Runtime is neither shut down again nor restarted.");
                Check(Released(cleanupOnly) && runtime.State == LiteRuntime.RuntimeState.ShutdownFailed,
                    "The cleanup-only branch also releases its fixtures without forging Idle or restoring a baseline from incomplete shutdown.");
            }
            finally
            {
                // This is the final lifecycle case. Avoid an application-quit retry on the intentionally terminal Runtime.
                UnityEngine.Object.DestroyImmediate(launcher.gameObject);
            }
        }

        private sealed class PriorityItem
        {
            internal int Priority;
            internal long Order;
        }

        private static UniTask PriorityQueueOrdering()
        {
            var queue = new LiteQuark.Runtime.PriorityQueue<PriorityItem>((a, b) => a.Priority.CompareTo(b.Priority));
            var fifo = Enumerable.Range(0, 6).Select(i => new PriorityItem { Order = i }).ToArray();
            foreach (var item in fifo) queue.Enqueue(item);
            Check(fifo.All(item => ReferenceEquals(item, queue.Dequeue())), "Six equal-priority items dequeue in original order.");
            fifo[0].Priority = -1; fifo[1].Priority = 1;
            queue.Enqueue(fifo[0]); queue.Enqueue(fifo[1]);
            fifo[0].Priority = 1; queue.UpdatePriority(fifo[0]);
            Check(ReferenceEquals(fifo[0], queue.Dequeue()), "Promotion preserves original enqueue age among equal priorities.");
            queue.Clear();
            var random = new System.Random(9421);
            var model = new List<PriorityItem>();
            long order = 0;
            for (var i = 0; i < 10000; i++)
            {
                var op = random.Next(6);
                if (model.Count == 0 || op == 0)
                {
                    var item = new PriorityItem { Priority = random.Next(-2, 3), Order = order++ };
                    queue.Enqueue(item); model.Add(item);
                }
                else if (op == 1)
                {
                    var item = model[random.Next(model.Count)];
                    item.Priority = random.Next(-2, 3);
                    if (!queue.UpdatePriority(item)) throw new Exception("Missing priority index.");
                }
                else if (op == 2)
                {
                    var item = model[random.Next(model.Count)];
                    if (!queue.Remove(item) || queue.Contains(item)) throw new Exception("Remove/index mismatch.");
                    model.Remove(item);
                }
                else if (op == 3)
                {
                    var expected = model.OrderByDescending(x => x.Priority).ThenBy(x => x.Order).First();
                    if (!ReferenceEquals(expected, queue.Dequeue())) throw new Exception("Unstable dequeue.");
                    model.Remove(expected);
                }
                else if (op == 4)
                {
                    queue.Clear(); model.Clear(); order = 0;
                }
                else
                {
                    var rejected = false;
                    try { queue.Enqueue(model[0]); } catch (ArgumentException) { rejected = true; }
                    if (!rejected) throw new Exception("Duplicate accepted.");
                }
                if (queue.Count != model.Count || model.Any(item => !queue.Contains(item))) throw new Exception("Index/count mismatch.");
                if (model.Count > 0 && !ReferenceEquals(queue.Peek(), model.OrderByDescending(x => x.Priority).ThenBy(x => x.Order).First()))
                    throw new Exception("Heap differs from stable reference model.");
            }
            Check(true, "10000 deterministic operations match stable reference ordering, including decrease, removal, clear and duplicates.");
            return UniTask.CompletedTask;
        }

        private static async UniTask PriorityLoaderScheduling()
        {
            var locator = new Locator(true);
            var bundles = Enumerable.Range(0, 6).Select(i => new BundleInfo(i, "queue" + i + ".ab", Array.Empty<string>(), null)).ToArray();
            var pack = new VersionPackInfo("priority", "windows", false, false, bundles);
            pack.Initialize();
            var loader = new AssetBundleLoader(locator, pack, 2);
            var callbacks = 0;
            try
            {
                foreach (var bundle in bundles) loader.LoadBundle(bundle, AssetLoadPriority.Normal, _ => callbacks++);
                loader.Tick(0);
                Check(locator.Started.SequenceEqual(new[] { "queue0.ab", "queue1.ab" }), "Two oldest equal-priority tasks start; remaining tasks stay queued.");
                loader.RaiseBundlePriority(bundles[0], AssetLoadPriority.High);
                loader.RaiseBundlePriority(bundles[4], AssetLoadPriority.High);
                loader.LoadBundle(bundles[4], AssetLoadPriority.Low, _ => callbacks++);
                loader.Tick(0);
                Check(locator.Started.Count == 2 && locator.Manual.Values.Count(t => !t.IsDone) == 2, "Promotion neither preempts running work nor exceeds the concurrency limit.");
                locator.Manual["queue0.ab"].Finish(null);
                Check(callbacks == 1 && locator.Started.Count == 2,
                    "Task completion delivers its result immediately but does not recursively start queued work.");
                loader.Tick(0);
                Check(locator.Started.Last() == "queue4.ab", "Shared queued request stays High despite a later Low request.");
                locator.Manual["queue1.ab"].Finish(null); loader.Tick(0);
                Check(locator.Started.Last() == "queue2.ab", "Remaining Normal work resumes FIFO.");
                for (var tick = 0; tick < 6 && callbacks < 7; tick++)
                {
                    locator.FinishOutstanding(); loader.Tick(0);
                    if (locator.Manual.Values.Count(t => !t.IsDone) > 2) throw new Exception("Concurrency exceeded.");
                }
                Check(locator.Started.SequenceEqual(new[] { "queue0.ab", "queue1.ab", "queue4.ab", "queue2.ab", "queue3.ab", "queue5.ab" }) &&
                    locator.Calls.Values.All(count => count == 1) && callbacks == 7,
                    "Each physical Bundle starts once; seven logical callbacks complete exactly once.");
            }
            finally { await loader.DisposeAsync(); }

            locator = new Locator(true);
            loader = new AssetBundleLoader(locator, pack, 1);
            callbacks = 0;
            var failure = new InvalidOperationException("Controlled Bundle failure");
            AssetLoadResult<IBundleResource> failedResult = null;
            try
            {
                loader.LoadBundle(bundles[0], AssetLoadPriority.Normal, result =>
                {
                    callbacks++;
                    failedResult = result;
                    loader.LoadBundle(bundles[0], AssetLoadPriority.Normal, _ => callbacks++);
                });
                loader.Tick(0);
                locator.Manual["queue0.ab"].Fail(failure);
                Check(callbacks == 1 && ReferenceEquals(failedResult.Error.Exception, failure) && locator.Started.Count == 1,
                    "Failure delivers the original error immediately; same-path retry stays queued until Tick.");
                loader.Tick(0);
                Check(locator.Calls["queue0.ab"] == 2 && callbacks == 1,
                    "Retiring the old failed task does not remove or notify the replacement request.");
                locator.Manual["queue0.ab"].Cancel();
                loader.Tick(0); loader.Tick(0);
                Check(callbacks == 2, "A cancelled task without a result callback ends its request once when retired.");
            }
            finally { await loader.DisposeAsync(); }
        }

        private static AssetLoadPriority CachePriority(Harness h, string path) => Get<AssetLoadPriority>(h.Cache(path), "_loadPriority");
        private static AssetHandle<T> PriorityAsset<T>(Harness h, string path, AssetLoadPriority priority) where T : UnityEngine.Object
        {
            var handle = h.Assets.LoadAssetHandle<T>(path, priority);
            h.Handles.Add(handle);
            return handle;
        }

        private static async UniTask PrioritySharedDependencies()
        {
            var h = New(true, 1);
            var blocker = h.Asset<TextAsset>(TextPath); h.Provider.Tick(0);
            var preload = h.Assets.PreloadAssetHandle<GameObject>(new[] { PrefabPath, "assets/__session_validation/c.prefab" }, AssetLoadPriority.Low);
            h.Handles.Add(preload);
            var high = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
            var low = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
            Check(CachePriority(h, "a.ab") == AssetLoadPriority.High && CachePriority(h, "b.ab") == AssetLoadPriority.High &&
                CachePriority(h, "c.ab") == AssetLoadPriority.Low, "Higher request promotes its main and shared dependency without promoting an unrelated parent.");
            Check(h.Refs("b.ab") == 2 && Get<List<Action<AssetLoadResult<IBundleResource>>>>(h.Cache("b.ab"), "_bundleLoaderCallbackList").Count == 2,
                "Promotion adds no dependency lease or completion subscription.");
            h.Finish("text.ab", true); h.Finish("b.ab", true); h.Finish("a.ab", true); h.Finish("c.ab", true);
            await Wait(() => blocker.IsDone && preload.IsDone && high.IsDone && low.IsDone, "Prioritized shared resources");
            Check(preload.Result && high.Result != null && high.Result == low.Result, "Merged requests deliver valid shared assets.");
            Check(h.Locater.Started.SequenceEqual(new[] { "text.ab", "b.ab", "a.ab", "c.ab" }) && h.Locater.Calls.Values.All(n => n == 1),
                "Physical starts follow High then Low, and the shared dependency loads once.");
        }

        private static async UniTask PriorityMainBeforeDependency(bool completeMain)
        {
            var h = New(true, 1);
            var first = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
            var pack = Get<VersionPackInfo>(h.Provider, "_packInfo");
            // Deliberately prioritize only the physical main to exercise the supported arbitrary Bundle ordering.
            h.Provider.RaiseBundlePriority(pack.GetBundleInfoFromBundlePath("a.ab"), AssetLoadPriority.High);
            h.Provider.Tick(0);
            Check(h.Locater.Started.SequenceEqual(new[] { "a.ab" }), "Main starts while its dependency remains queued.");
            AssetHandle<TextAsset> blocker = null;
            if (completeMain)
            {
                blocker = h.Asset<TextAsset>(TextPath);
                h.Finish("a.ab", true);
                Check(h.Locater.Started.Last() == "text.ab" && !first.IsDone && h.Cache("a.ab").Stage == AssetCacheStage.Loading,
                    "Physical main is complete, but Cache and asset remain pending on queued dependency.");
            }
            var second = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
            Check(CachePriority(h, "b.ab") == AssetLoadPriority.High && h.Locater.Calls["a.ab"] == 1 && h.Refs("b.ab") == 1,
                "Promotion reaches dependency without restarting main or duplicating its dependency lease.");
            if (completeMain) h.Finish("text.ab", true); else h.Finish("a.ab", true);
            Check(!first.IsDone && !second.IsDone && h.Locater.Started.Last() == "b.ab", "Asset extraction still waits for the dependency Bundle.");
            h.Finish("b.ab", true);
            await Wait(() => first.IsDone && second.IsDone && (blocker == null || blocker.IsDone), "Main-before-dependency delivery");
            Check(first.Result != null && first.Result == second.Result && first.Result.GetComponent<Renderer>().sharedMaterial != null,
                "Arbitrary Bundle order preserves real dependency references and shared asset delivery.");
        }

        private static async UniTask PriorityCanceledRequest()
        {
            var h = New(true, 1);
            var blocker = h.Asset<TextAsset>(TextPath); h.Provider.Tick(0);
            var high = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
            high.Dispose();
            var low = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
            Check(CachePriority(h, "a.ab") == AssetLoadPriority.High && CachePriority(h, "b.ab") == AssetLoadPriority.High,
                "Disposing High and adding Low retain this loading round's maximum priority.");
            h.Finish("text.ab", true); h.Finish("b.ab", true); h.Finish("a.ab", true);
            await Wait(() => blocker.IsDone && high.IsDone && low.IsDone, "Canceled prioritized request cleanup");
            Check(high.Result == null && low.Result != null && h.AssetRefs("a.ab", PrefabPath, typeof(GameObject)) == 1 && h.Refs("b.ab") == 1,
                "Late canceled result releases its own lease while the remaining consumer keeps valid ownership.");
        }

        private static async UniTask PriorityRetry()
        {
            var h = New(true, 1);
            var first = PriorityAsset<TextAsset>(h, TextPath, AssetLoadPriority.High);
            h.Provider.Tick(0); h.Finish("text.ab", false);
            Check(first.IsDone && first.Result == null && h.Cache("text.ab").Stage == AssetCacheStage.Idle, "Failed High round returns to Idle.");
            var retry = PriorityAsset<TextAsset>(h, TextPath, AssetLoadPriority.Low);
            var normal = h.Asset<Material>(MaterialPath);
            Check(CachePriority(h, "text.ab") == AssetLoadPriority.Low, "New Low round overwrites the previous High priority.");
            h.Provider.Tick(0);
            Check(h.Locater.Started.Last() == "b.ab", "New Normal request starts before Low retry.");
            h.Finish("b.ab", true); h.Finish("text.ab", true);
            await Wait(() => retry.IsDone && normal.IsDone, "Priority reset retry");
            Check(retry.Result != null && normal.Result != null && h.Locater.Calls["text.ab"] == 2, "Retry succeeds through a fresh physical Bundle operation.");
        }

        private static async UniTask PriorityPublicEntryPoints()
        {
            var h = New(true);
            var preload = h.Assets.PreloadAssetHandle<TextAsset>(new[] { TextPath, TextPath }, AssetLoadPriority.Low);
            var first = h.Assets.InstantiateHandle(PrefabPath, null);
            var second = h.Assets.InstantiateHandle("assets/__session_validation/c.prefab", null, Vector3.one, Quaternion.identity, AssetLoadPriority.High);
            var scene = h.Assets.LoadSceneHandle(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive), AssetLoadPriority.High);
            h.Handles.Add(preload); h.Handles.Add(first); h.Handles.Add(second); h.Handles.Add(scene);
            Check(CachePriority(h, "text.ab") == AssetLoadPriority.Low && CachePriority(h, "a.ab") == AssetLoadPriority.Normal &&
                CachePriority(h, "c.ab") == AssetLoadPriority.High && CachePriority(h, "scene.ab") == AssetLoadPriority.High &&
                CachePriority(h, "b.ab") == AssetLoadPriority.High, "Preload, default instantiate, positioned instantiate and scene propagate their priorities.");
            h.Provider.Tick(0);
            foreach (var path in h.Locater.Started.ToArray()) h.Finish(path, true);
            await Wait(() => preload.IsDone && first.IsDone && second.IsDone && scene.IsDone, "All public priority entry points");
            Check(preload.Result && preload.TotalCount == 1 && first.Result != null && second.Result != null && scene.Result.HasValue,
                "All entry points deliver real assets/instances/scene; preload still deduplicates.");
            Check(second.Result.transform.position == Vector3.one, "Positioned instantiate retains existing placement behavior.");
        }

        private static List<ITask> TaskSnapshot()
        {
            var tasks = new List<ITask>();
            LiteRuntime.Task.CopyTasksTo(tasks);
            return tasks;
        }

        private static TaskGroup RuntimeGroup(int id) =>
            Get<List<TaskGroup>>(LiteRuntime.Task, "_groups").Single(group => group.Id == id);

        private static List<TaskRecord> RuntimeRecords() =>
            Get<List<TaskRecord>>(LiteRuntime.Task, "_records");

        private static List<GateTask> FillGroup(int groupId, int count)
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

        private static async UniTask ReleaseGates(List<GateTask> gates)
        {
            foreach (var task in gates) task.Cancel();
            await Wait(() => gates.All(task => task.Disposed), "Gate tasks retired");
        }

        private static UniTask AssetGroupConfiguration()
        {
            var groups = Get<List<TaskGroup>>(LiteRuntime.Task, "_groups");
            Check(groups.Select(group => group.Id).Distinct().Count() == groups.Count,
                "Framework group IDs are unique after real AssetSystem initialization.");
            Check(RuntimeGroup(LiteConst.TaskGroupId.Default).ConcurrencyLimit == Math.Max(1, LiteRuntime.Setting.Task.ConcurrencyLimit)
                && RuntimeGroup(LiteConst.TaskGroupId.Asset).ConcurrencyLimit == Math.Max(1, LiteRuntime.Setting.Asset.TaskConcurrencyLimit)
                && RuntimeGroup(LiteConst.TaskGroupId.Flow).ConcurrencyLimit == null,
                "Default and Asset read their separate settings; Flow remains unbounded.");
            Check(LiteConst.TaskGroupId.Default == 0 && LiteConst.TaskGroupId.Flow == -1 && LiteConst.TaskGroupId.Asset == -2,
                "The new centralized reserved group IDs are used by the running framework.");
            return UniTask.CompletedTask;
        }

        private static async UniTask AssetGroupDefaultSaturation()
        {
            var h = New(false, 2);
            var group = RuntimeGroup(LiteConst.TaskGroupId.Default);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value);
            GateTask queued = null;
            try
            {
                await Wait(() => gates.All(task => task.IsExecuted), "Default saturation");
                queued = new GateTask();
                LiteRuntime.Task.AddTask(queued, group.Id, TaskPriority.Urgent);
                Check(group.RunningTaskCount == group.ConcurrencyLimit && !queued.IsExecuted,
                    "Default is full and an additional Urgent business task remains queued.");

                var text = h.Asset<TextAsset>(TextPath);
                var prefab = h.Asset<GameObject>(PrefabPath);
                var scene = h.Scene();
                await Wait(() => text.IsDone && prefab.IsDone && scene.IsDone,
                    "Real Asset group pipeline while Default remains full", 20);
                Check(text.Result != null && prefab.Result != null
                    && prefab.Result.GetComponent<Renderer>().sharedMaterial != null
                    && scene.Result.HasValue && scene.Result.Value.isLoaded,
                    "Real local Bundles, shared dependency, text, prefab and scene finish despite saturated Default.");
                Check(gates.All(task => task.State == TaskState.Running) && !queued.IsExecuted
                    && group.RunningTaskCount == group.ConcurrencyLimit,
                    "Resource completion did not borrow or release any Default slot; Urgent still does not bypass its group.");

                var loadedScene = scene.Result.Value;
                var entry = Get<SceneInfoCache>(Get<SceneInfoCache.Lease>(scene, "_lease"), "_cache");
                scene.Dispose();
                var unloadOperation = Get<AsyncOperation>(entry, "_unloadOperation");
                Check(unloadOperation != null && !TaskSnapshot().OfType<AsyncOperationTask>().Any(task =>
                        ReferenceEquals(Get<AsyncOperation>(task, "_asyncOperation"), unloadOperation)),
                    "Scene unload uses its native completion callback without occupying a task group.");
                await Wait(() => unloadOperation.isDone && h.Refs("scene.ab") == 0
                    && (!loadedScene.IsValid() || !loadedScene.isLoaded),
                    "Native scene unload completes with Default full", 15);
                Check(!queued.IsExecuted && h.Refs("scene.ab") == 0,
                    "Scene unload and lease return finish without Default admission.");
            }
            finally
            {
                queued?.Cancel();
                await ReleaseGates(gates);
            }
        }

        private static async UniTask AssetGroupFactoryRouting()
        {
            var h = New();
            var group = RuntimeGroup(LiteConst.TaskGroupId.Asset);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value);
            var tasks = new List<ITask>();
            AssetBundle bundle = null;
            try
            {
                await Wait(() => gates.All(task => task.IsExecuted), "Asset group saturation");
                bundle = AssetBundle.LoadFromFile(Path.Combine(BundleFolder, "text.ab"));
                Check(bundle != null, "Factory routing fixture is a real loaded Bundle.");
                tasks.Add(LiteRuntime.Task.AddLoadVersionPackTask(new Uri(Path.Combine(BundleFolder, "unused-version.bytes")).AbsoluteUri, null));
                tasks.Add(LiteRuntime.Task.AddLoadLocalBundleTask(Path.Combine(BundleFolder, "text.ab"), null));
                tasks.Add(LiteRuntime.Task.AddLoadRemoteBundleTask("http://127.0.0.1/validation-not-sent.ab", null, null));
                tasks.Add(LiteRuntime.Task.AddLoadAssetTask<TextAsset>(bundle, "payload.txt", null));
                tasks.Add(LiteRuntime.Task.AddLoadSceneTask("validation_scene", new LoadSceneParameters(LoadSceneMode.Additive), null));
                for (var i = 0; i < tasks.Count; i++)
                {
                    var record = RuntimeRecords().Single(value => ReferenceEquals(value.Task, tasks[i]));
                    Check(record.Group.Id == LiteConst.TaskGroupId.Asset
                        && record.Priority == (i < 3 ? TaskPriority.Urgent : TaskPriority.Normal),
                        tasks[i].GetType().Name + " uses Asset and its unchanged fixed priority.");
                }

                var defaultRan = false;
                var flowRan = false;
                var defaultTask = new MainThreadTask(_ => defaultRan = true, null);
                LiteRuntime.Task.AddTask(defaultTask);
                LiteRuntime.Task.AddMainThreadTask(_ => flowRan = true, null);
                await Wait(() => defaultRan && flowRan, "Other groups execute while Asset is full");
                Check(tasks.All(task => !task.IsExecuted && task.State == TaskState.Pending)
                    && group.RunningTaskCount == group.ConcurrencyLimit,
                    "All five factories wait for Asset capacity, including Urgent; Default and Flow remain runnable.");
            }
            finally
            {
                foreach (var task in tasks) task.Cancel();
                await ReleaseGates(gates);
                if (bundle != null) bundle.Unload(true);
            }
        }

        private static async UniTask AssetGroupLoaderBudgets(int availableSlots)
        {
            const int loaderLimit = 2;
            Current = new Harness(true, loaderLimit, scheduleManual: true);
            var h = Current;
            var group = RuntimeGroup(LiteConst.TaskGroupId.Asset);
            var gates = FillGroup(group.Id, group.ConcurrencyLimit.Value - availableSlots);
            try
            {
                await Wait(() => gates.All(task => task.IsExecuted), "Reserve Asset slots");
                var text = h.Asset<TextAsset>(TextPath);
                var material = h.Asset<Material>(MaterialPath);
                var low = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.Low);
                var normal = PriorityAsset<GameObject>(h, "assets/__session_validation/c.prefab", AssetLoadPriority.Normal);
                h.Provider.Tick(0);
                LiteRuntime.Task.Tick(0);
                var admitted = h.Locater.Manual.Values.Count(task => task.IsExecuted && !task.IsDone);
                Check(h.Locater.Started.SequenceEqual(new[] { "text.ab", "b.ab" })
                    && admitted == Math.Min(availableSlots, loaderLimit),
                    $"Loader submits only {loaderLimit} tasks; with {availableSlots} Asset slots, exactly {Math.Min(availableSlots, loaderLimit)} are admitted.");

                var high = PriorityAsset<GameObject>(h, PrefabPath, AssetLoadPriority.High);
                h.Provider.Tick(0);
                Check(h.Locater.Started.Count == loaderLimit && CachePriority(h, "a.ab") == AssetLoadPriority.High
                    && CachePriority(h, "b.ab") == AssetLoadPriority.High,
                    "Shared dependency promotion retains Loader capacity and does not restart submitted work.");
                Check(h.Locater.Manual.Values.All(task => RuntimeRecords().Single(record => ReferenceEquals(record.Task, task)).Priority == TaskPriority.Urgent),
                    "Already submitted Bundle records retain fixed Urgent priority after the Asset request is promoted.");

                var maxLoader = 0;
                var maxAdmitted = admitted;
                var deadline = Time.realtimeSinceStartup + 20;
                while (!(text.IsDone && material.IsDone && low.IsDone && normal.IsDone && high.IsDone))
                {
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Dual-budget loading");
                    foreach (var pair in h.Locater.Manual.ToArray())
                    {
                        if (pair.Value.IsExecuted && !pair.Value.IsDone) h.Finish(pair.Key, true);
                    }
                    await Frame();
                    var loader = Get<AssetBundleLoader>(h.Provider, "_bundleLoader");
                    var submitted = Get<System.Collections.IList>(loader, "_runningTasks").Count;
                    admitted = h.Locater.Manual.Values.Count(task => task.IsExecuted && !task.IsDone);
                    maxLoader = Math.Max(maxLoader, submitted);
                    maxAdmitted = Math.Max(maxAdmitted, admitted);
                    if (submitted > loaderLimit || admitted > availableSlots || group.RunningTaskCount > group.ConcurrencyLimit)
                        throw new Exception("Loader or Asset group quota exceeded.");
                }
                Check(h.Locater.Started.SequenceEqual(new[] { "text.ab", "b.ab", "a.ab", "c.ab" })
                    && h.Locater.Calls.Values.All(count => count == 1),
                    "Loader promotion orders queued High before Normal, preserves initial FIFO, and submits each shared Bundle once.");
                Check(text.Result != null && material.Result != null && low.Result != null
                    && low.Result == high.Result && normal.Result != null,
                    "Controlled Bundle completions deliver real assets through the unchanged cache/lease pipeline.");
                Check(maxLoader <= loaderLimit && maxAdmitted <= Math.Min(availableSlots, loaderLimit),
                    $"Both limits remain independent throughout the run: Loader peak {maxLoader}, admitted Bundle peak {maxAdmitted}.");
            }
            finally
            {
                await ReleaseGates(gates);
            }
        }

        private sealed class GateTask : BaseTask
        {
            internal bool Disposed;
            protected override void OnExecute() { }
            protected override void OnDispose() { Disposed = true; }
        }
    }
}
