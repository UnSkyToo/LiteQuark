using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using System.Runtime.ExceptionServices;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private sealed class RestartFixture
        {
            internal readonly LiteSetting OriginalSetting = LiteRuntime.Setting;
            internal readonly GameObject Camera = new("RestartUICamera", typeof(Camera));
            internal readonly string Scratch = Path.Combine(Root, "Scratch", "restart_" + Guid.NewGuid().ToString("N"));
            internal readonly List<string> Events = new();
            internal readonly List<RestartSystemProbe> Systems = new();
            internal readonly List<RestartLogicProbe> Logics = new();
            internal readonly UniTaskCompletionSource DisposeGate = new();
            internal readonly List<Exception> Errors = new();
            internal List<GateTask> Gates;
            internal ExceptionDispatchInfo Failure;
            internal int CleanupShutdownCalls;
            internal int BaselineStartups;
            internal AssetSystem OldAsset;
            internal TaskSystem OldTask;
            internal bool RequestRestart;
            internal bool PreviousRoundReleased;
            internal string PlatformFolder => Path.Combine(Scratch, AppUtils.GetCurrentPlatformName().ToLowerInvariant());
            internal string ContentFolder => Path.Combine(PlatformFolder, AppUtils.GetMainVersion());

            internal RestartFixture(Action<ProbeUI> onOpen)
            {
                ProbeUI.Opening = onOpen;
                UniTaskScheduler.UnobservedTaskException += OnUnobserved;
            }

            internal void CaptureFailure(Exception error)
            {
                if (Failure == null) Failure = ExceptionDispatchInfo.Capture(error);
                else if (!ReferenceEquals(Failure.SourceException, error)) Errors.Add(error);
            }

            internal void OnUnobserved(Exception error)
            {
                // Restart is fire-and-forget; retain its original exception without suppressing the normal error log.
                CaptureFailure(error);
                Debug.LogException(error);
            }
        }

        private static RestartFixture _restartFixture;

        private sealed class RestartSystemProbe : ISystem
        {
            private readonly RestartFixture _fixture = _restartFixture;
            internal readonly int Generation;
            internal int Disposals;

            public RestartSystemProbe()
            {
                Generation = _fixture.Systems.Count + 1;
                if (Generation == 2)
                {
                    _fixture.PreviousRoundReleased = _fixture.Events.Last() == "system 1 disposed" &&
                        Get<IAssetManager>(_fixture.OldAsset, "_manager") == null &&
                        Get<bool>(_fixture.OldTask, "_isClosing") && Get<List<TaskGroup>>(_fixture.OldTask, "_groups").Count == 0;
                }
                _fixture.Systems.Add(this);
                _fixture.Events.Add("system " + Generation + " constructed");
            }

            public UniTask<bool> Initialize() => UniTask.FromResult(true);

            public async UniTask DisposeAsync()
            {
                Disposals++;
                _fixture.Events.Add("system " + Generation + " disposing");
                if (Generation == 1) await _fixture.DisposeGate.Task;
                _fixture.Events.Add("system " + Generation + " disposed");
            }
        }

        private sealed class RestartLogicProbe : ILogic
        {
            private readonly RestartFixture _fixture = _restartFixture;
            internal readonly int Generation;
            internal int Disposals;
            internal int TicksAfterDispose;

            public RestartLogicProbe()
            {
                Generation = _fixture.Logics.Count + 1;
                _fixture.Logics.Add(this);
            }

            public UniTask<bool> Initialize()
            {
                _fixture.Events.Add("logic " + Generation + " initialized");
                return UniTask.FromResult(true);
            }

            public void Tick(float deltaTime)
            {
                if (Disposals > 0) TicksAfterDispose++;
                if (Generation != 1 || !_fixture.RequestRestart) return;
                _fixture.RequestRestart = false;
                _fixture.Events.Add("logic 1 restart requested");
                LiteRuntime.Instance.Restart();
            }

            public UniTask DisposeAsync()
            {
                Disposals++;
                _fixture.Events.Add("logic " + Generation + " disposed");
                return UniTask.CompletedTask;
            }
        }

        private static async UniTask RuntimeRestartPendingUI()
        {
            var runtime = LiteRuntime.Instance;
            var opened = new List<ProbeUI>();
            var updates = 0;
            var closes = 0;
            var fixture = new RestartFixture(ui =>
            {
                opened.Add(ui);
                ui.Updating = () => updates++;
                ui.Closing = () => closes++;
            });
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
                setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = typeof(UISystem).AssemblyQualifiedName });
                setting.LogicList.Add(new LiteTypeEntryData<ILogic> { AssemblyQualifiedName = typeof(RestartLogicProbe).AssemblyQualifiedName });
                setting.SystemSettings.Add(new UISystemSetting { UICamera = fixture.Camera.GetComponent<Camera>() });

                await runtime.Shutdown();
                _restartFixture = fixture;
                runtime.Startup(runtime.Launcher, setting);
                await WaitWithoutLoaderTick(() => runtime.State == LiteRuntime.RuntimeState.Running, "Restart fixture startup");
                var oldSystems = Get<List<ISystem>>(SystemCenter.Instance, "_systemList").ToArray();
                var oldUI = LiteRuntime.Get<UISystem>();
                var oldRoot = oldUI.CanvasRoot;
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
                var pending = Open(oldUI).Preserve();
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
                Check(cancelled && ReferenceEquals(oldUI.CanvasRoot, null),
                    "The old OpenUI propagates cancellation and its registered UISystem clears Canvas ownership.");

                fixture.Gates[0].Cancel();
                await WaitWithoutLoaderTick(() => entry.IsLoaded, "Late real prefab extraction after UI cancellation");
                await UniTask.NextFrame();
                Check(assetTask.State == TaskState.Succeeded && entry.Asset is GameObject && Get<int>(entry, "_refCount") == 0 && opened.Count == 0,
                    "The cancelled UI rejects an actual late successful prefab result and returns its lease without calling OnOpen.");
                Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown && fixture.Systems.Count == 1 && fixture.Logics.Count == 1 && oldRoot == null,
                    "Old Canvas destruction completes while the outstanding system disposal still prevents any new round.");

                fixture.DisposeGate.TrySetResult();
                await WaitWithoutLoaderTick(() => runtime.State is LiteRuntime.RuntimeState.Error or LiteRuntime.RuntimeState.ShutdownFailed ||
                    fixture.Logics.Count == 2 && runtime.State == LiteRuntime.RuntimeState.Running, "New Runtime generation Running");
                fixture.Failure?.Throw();
                var newUI = LiteRuntime.Get<UISystem>();
                var newRoot = newUI.CanvasRoot;
                var newSystems = Get<List<ISystem>>(SystemCenter.Instance, "_systemList");
                Check(fixture.PreviousRoundReleased && fixture.Events.SequenceEqual(new[] { "system 1 constructed", "logic 1 initialized", "logic 1 restart requested",
                    "logic 1 disposed", "system 1 disposing", "system 1 disposed", "system 2 constructed", "logic 2 initialized" }) &&
                    fixture.Systems[0].Disposals == 1 && fixture.Logics[0].Disposals == 1 && fixture.Logics[0].TicksAfterDispose == 0,
                    "Old Logic and system disposal finish exactly once, including Asset and Task cleanup, before the next probe is constructed.");
                Check(newSystems.Count == oldSystems.Length && newSystems.All(system => !oldSystems.Contains(system)) &&
                    !ReferenceEquals(LiteRuntime.Asset, fixture.OldAsset) && !ReferenceEquals(LiteRuntime.Task, fixture.OldTask) && !ReferenceEquals(newUI, oldUI) &&
                    oldBundles.All(bundle => bundle.Bundle == null) && Get<Dictionary<string, AssetBundleCache>>(provider, "_bundleCacheMap").Count == 0,
                    "Every registered System is replaced, and the old Provider unloads both the prefab Bundle and its dependency.");

                var request = Open(newUI).Preserve();
                await WaitWithoutLoaderTick(() => request.Status.IsCompleted(), "New UISystem opens the same prefab");
                var ui = await request;
                var go = ui == null ? null : ui.Go;
                Check(ui != null && opened.Count == 1 && ReferenceEquals(opened[0], ui) && ReferenceEquals(ui.System, newUI) &&
                    go != null && go.transform.IsChildOf(newRoot) && go.GetComponent<Renderer>().sharedMaterial != null,
                    "The new round reloads the same prefab and dependency, calls only the new OnOpen, and parents it under the new Canvas.");
                await WaitWithoutLoaderTick(() => updates > 0, "New UI participates in Runtime Tick");
                newUI.CloseUI(ui);
                await WaitWithoutLoaderTick(() => go == null, "New UI deferred close destruction");
                var newProvider = Get<AssetManager>(LiteRuntime.Asset, "_manager");
                var newEntry = Get<Dictionary<(string, Type), AssetInfoCache>>(newProvider.GetOrCreateBundleCache("a.ab"), "_assetCacheMap")[(PrefabPath, typeof(GameObject))];
                Check(closes == 1 && ui.State == UIState.Closed &&
                    ui.Go == null && Get<int>(newEntry, "_refCount") == 0,
                    "The new UISystem updates and closes the UI through normal Runtime-driven queues, returning its instance and prefab lease.");
                var roots = UnityEngine.Object.FindObjectsOfType<RectTransform>(true).Where(root => root.parent == null && root.name == "Canvas").ToArray();
                Check(oldRoot == null && roots.Length == 1 && roots[0] == newRoot && opened.Count == 1 && fixture.Logics[0].TicksAfterDispose == 0,
                    "After delayed destruction, only the new round's Canvas root remains and the old round stays inactive.");
                runtime.EnterErrorState(FrameworkErrorCode.Unknown);
                Check(runtime.State == LiteRuntime.RuntimeState.Error,
                    "A running generation can enter Error before fixture teardown.");
            }
            catch (Exception ex) { fixture.CaptureFailure(ex); }
            finally { await FinishRestartFixture(fixture); }
            Check(fixture.CleanupShutdownCalls == 1 && fixture.BaselineStartups == 1 &&
                runtime.State == LiteRuntime.RuntimeState.Running && ReferenceEquals(LiteRuntime.Setting, fixture.OriginalSetting),
                "Fixture cleanup closes an ordinary Error once and restores the original running configuration.");
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
                    throw new InvalidOperationException("Restart fixture shutdown failed; baseline startup is skipped.");
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
            ProbeUI.Opening = null;
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
    }
}
