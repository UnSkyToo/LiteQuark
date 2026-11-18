using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static partial class TaskSystemValidationRunner
    {
        private sealed class ShutdownFixture
        {
            internal readonly List<string> Events = new();
            internal readonly List<string> Initialized = new();
            internal readonly Dictionary<string, ShutdownProbe> Instances = new();
            internal readonly UniTaskCompletionSource LogicGate = new();
            internal readonly UniTaskCompletionSource SystemGate = new();
            internal UniTaskCompletionSource<bool> InitializationGate;
            internal string GatedInitialization = "system 2";
            internal string FailedInitialization;
            internal string FailedDisposal;
            internal readonly Exception DisposalError = new InvalidOperationException("InjectedValidation partial system disposal");
            internal bool ThrowOnInitialize;
            internal bool InTimerCallback;
            internal bool DisposalStartedInTimerCallback;
            internal int TicksAfterDispose;
        }

        private static ShutdownFixture _shutdownFixture;

        private abstract class ShutdownProbe : ILogic, ISystem
        {
            private readonly string _name;
            private readonly ShutdownFixture _fixture;
            private bool _disposed;
            internal int Disposals;
            internal int Ticks;

            protected ShutdownProbe(string name)
            {
                _name = name;
                _fixture = _shutdownFixture;
                _fixture.Instances.Add(name, this);
            }

            public async UniTask<bool> Initialize()
            {
                _fixture.Initialized.Add(_name);
                if (_name == _fixture.FailedInitialization)
                {
                    if (_fixture.ThrowOnInitialize)
                        throw new InvalidOperationException("InjectedValidation runtime initialization");
                    return false;
                }
                if (_name == _fixture.GatedInitialization && _fixture.InitializationGate != null)
                    return await _fixture.InitializationGate.Task;
                return true;
            }

            public void Tick(float deltaTime)
            {
                Ticks++;
                if (_disposed) _fixture.TicksAfterDispose++;
            }

            public async UniTask DisposeAsync()
            {
                Disposals++;
                _fixture.DisposalStartedInTimerCallback |= _fixture.InTimerCallback;
                _fixture.Events.Add(_name + " start");
                if (_name == "logic 1") await _fixture.LogicGate.Task;
                if (_name == "system 2") await _fixture.SystemGate.Task;
                _disposed = true;
                if (_name == _fixture.FailedDisposal) throw _fixture.DisposalError;
                _fixture.Events.Add(_name + " end");
            }
        }

        private sealed class ShutdownSystemOne : ShutdownProbe
        {
            public ShutdownSystemOne() : base("system 1") { }
        }

        private sealed class ShutdownSystemTwo : ShutdownProbe
        {
            public ShutdownSystemTwo() : base("system 2") { }
        }

        private sealed class ShutdownLogicOne : ShutdownProbe
        {
            public ShutdownLogicOne() : base("logic 1") { }
        }

        private sealed class ShutdownLogicTwo : ShutdownProbe
        {
            public ShutdownLogicTwo() : base("logic 2") { }
        }

        private static async UniTask BeginShutdownFixture(ShutdownFixture fixture)
        {
            var runtime = LiteRuntime.Instance;
            if (runtime.State != LiteRuntime.RuntimeState.Idle) await runtime.Shutdown();

            _shutdownFixture = fixture;
            var setting = new LiteSetting();
            setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = typeof(ShutdownSystemOne).AssemblyQualifiedName });
            setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = typeof(ShutdownSystemTwo).AssemblyQualifiedName });
            setting.LogicList.Add(new LiteTypeEntryData<ILogic> { AssemblyQualifiedName = typeof(ShutdownLogicOne).AssemblyQualifiedName });
            setting.LogicList.Add(new LiteTypeEntryData<ILogic> { AssemblyQualifiedName = typeof(ShutdownLogicTwo).AssemblyQualifiedName });
            runtime.Startup(runtime.Launcher, setting);
        }

        private static async UniTask WaitForRuntimeCondition(Func<bool> condition, string operation)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(operation);
                // The runner disables Launcher.Update; keep the real Runtime driving its systems.
                LiteRuntime.Instance.Tick(0f);
                await UniTask.NextFrame();
            }
        }

        private static async UniTask FinishShutdownFixture(ShutdownFixture fixture, UniTask? shutdown = null)
        {
            try
            {
                fixture.InitializationGate?.TrySetResult(true);
                fixture.LogicGate.TrySetResult();
                fixture.SystemGate.TrySetResult();

                var runtime = LiteRuntime.Instance;
                if (!shutdown.HasValue && runtime.State is not LiteRuntime.RuntimeState.Idle
                    and not LiteRuntime.RuntimeState.ShuttingDown and not LiteRuntime.RuntimeState.ShutdownFailed)
                    shutdown = runtime.Shutdown();

                if (shutdown.HasValue)
                {
                    var pending = shutdown.Value;
                    await WaitForRuntimeCondition(() => pending.Status != UniTaskStatus.Pending, "Fixture shutdown completion");
                    await pending;
                }
                else
                {
                    await WaitForRuntimeCondition(() => runtime.State != LiteRuntime.RuntimeState.ShuttingDown, "Automatic failure cleanup");
                }

                if (runtime.State != LiteRuntime.RuntimeState.Idle)
                    throw new Exception("Fixture cleanup did not finish in Idle: " + runtime.State);
            }
            finally
            {
                _shutdownFixture = null;
            }
        }

        private static async UniTask RuntimeShutdownOrder(TaskSystem system)
        {
            var fixture = new ShutdownFixture();
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Running, "Probe startup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2", "logic 1", "logic 2" }) &&
                    LiteRuntime.Get<ShutdownSystemOne>() != null && LiteRuntime.Get<ShutdownSystemTwo>() != null,
                    "Probes initialize through Runtime configuration and publish their systems.");

                shutdown = LiteRuntime.Instance.Shutdown();
                Check(fixture.Events.Count == 0 && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Shutdown defers disposal until the initialization callback stack has exited.");
                await WaitForRuntimeCondition(() => fixture.Events.Count > 0, "First logic disposal");
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Runtime shutdown waits for the first asynchronous logic before starting later cleanup.");
                await UniTask.NextFrame();
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "The pending logic continues to hold shutdown across a player loop.");

                LiteRuntime.Instance.EnterErrorState(FrameworkErrorCode.Unknown);
                Check(LiteRuntime.Instance.State == LiteRuntime.RuntimeState.ShuttingDown,
                    "Reporting an error does not overwrite the active shutdown state.");

                LiteRuntime.Instance.IsPause = true;
                LiteRuntime.Setting.Common.TimeScale = 0f;
                LiteRuntime.Timer.NextFrame(() =>
                {
                    fixture.InTimerCallback = true;
                    try { fixture.LogicGate.TrySetResult(); }
                    finally { fixture.InTimerCallback = false; }
                });
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start", "logic 1 end" }),
                    "Paused shutdown still drives Timer, but does not start the next disposal inside its callback.");

                await WaitForRuntimeCondition(() => fixture.Events.Contains("system 2 start"), "First system disposal");
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start", "logic 1 end", "logic 2 start", "logic 2 end", "system 2 start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Logic finishes in registration order before reverse-order system disposal begins.");
                await UniTask.NextFrame();
                Check(fixture.Events.Count == 5 && shutdown.Value.Status == UniTaskStatus.Pending,
                    "The pending system holds shutdown across a player loop.");

                LiteRuntime.Timer.NextFrame(() =>
                {
                    fixture.InTimerCallback = true;
                    try { fixture.SystemGate.TrySetResult(); }
                    finally { fixture.InTimerCallback = false; }
                });
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.Count == 6 && LiteRuntime.Get<ShutdownSystemTwo>() == null &&
                    !fixture.DisposalStartedInTimerCallback && fixture.TicksAfterDispose == 0,
                    "A disposed system is unpublished and skipped by the remaining Tick before the next system is released.");
            }
            finally
            {
                await FinishShutdownFixture(fixture, shutdown);
            }

            Check(fixture.Events.SequenceEqual(new[] { "logic 1 start", "logic 1 end", "logic 2 start", "logic 2 end", "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
                LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle,
                "Shutdown finishes the remaining systems in reverse order before returning to Idle.");
            Check(fixture.Instances.Values.All(probe => probe.Disposals == 1) && fixture.TicksAfterDispose == 0,
                "Every probe is disposed exactly once and no released probe is ticked.");
        }

        private static async UniTask RuntimeInitializationReturnsFalse(TaskSystem system)
        {
            var fixture = new ShutdownFixture { FailedInitialization = "system 2" };
            fixture.SystemGate.TrySetResult();
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle, "False initialization cleanup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2" }) && fixture.Instances.Count == 2,
                    "A false system initialization prevents later logic construction.");
                Check(fixture.Events.SequenceEqual(new[] { "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
                    fixture.Instances.Values.All(probe => probe.Disposals == 1),
                    "The unpublished failed system and previously initialized system are both disposed exactly once.");
                Check(LiteRuntime.Get<ShutdownSystemOne>() == null && LiteRuntime.Get<ShutdownSystemTwo>() == null,
                    "Failed initialization leaves no published probe system after cleanup.");
            }
            finally { await FinishShutdownFixture(fixture); }
        }

        private static async UniTask RuntimeInitializationThrows(TaskSystem system)
        {
            var fixture = new ShutdownFixture { FailedInitialization = "logic 2", ThrowOnInitialize = true };
            fixture.LogicGate.TrySetResult();
            fixture.SystemGate.TrySetResult();
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle, "Throwing initialization cleanup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2", "logic 1", "logic 2" }),
                    "The throwing logic is created through the real initialization path.");
                Check(fixture.Events.SequenceEqual(new[] { "logic 2 start", "logic 2 end", "logic 1 start", "logic 1 end", "system 2 start", "system 2 end", "system 1 start", "system 1 end" }),
                    "Unpublished failed logic is cleaned before ready logic and reverse-order systems.");
                Check(fixture.Instances.Values.All(probe => probe.Disposals == 1),
                    "Initialization exceptions do not omit or duplicate disposal.");
            }
            finally { await FinishShutdownFixture(fixture); }
        }

        private static async UniTask RuntimeShutdownDuringInitialization(TaskSystem system)
        {
            await CheckShutdownDuringInitialization(quitting: false);
            await CheckShutdownDuringInitialization(quitting: true);
        }

        private static async UniTask CheckShutdownDuringInitialization(bool quitting)
        {
            var fixture = new ShutdownFixture { InitializationGate = new UniTaskCompletionSource<bool>() };
            fixture.SystemGate.TrySetResult();
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => fixture.Initialized.Contains("system 2"), "Gated system initialization after Asset manifest collection");
                Check(LiteRuntime.Instance.State == LiteRuntime.RuntimeState.InitSystem &&
                    fixture.Initialized.Contains("system 2") && LiteRuntime.Get<ShutdownSystemTwo>() == null,
                    "The initializing system is owned but not published.");

                if (quitting) LiteRuntime.Instance.OnApplicationQuit();
                else shutdown = LiteRuntime.Instance.Shutdown();
                await UniTask.NextFrame();
                Check(fixture.Events.Count == 0 && LiteRuntime.Instance.State == LiteRuntime.RuntimeState.ShuttingDown &&
                    (!shutdown.HasValue || shutdown.Value.Status == UniTaskStatus.Pending),
                    "Shutdown or application quit waits for the current Initialize to return.");
                LiteRuntime.Instance.IsPause = true;
                LiteRuntime.Setting.Common.TimeScale = 0f;
                LiteRuntime.Timer.NextFrame(() => fixture.InitializationGate.TrySetResult(true));
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.SequenceEqual(new[] { "system 2 start", "system 2 end" }) && LiteRuntime.Get<ShutdownSystemTwo>() == null &&
                    LiteRuntime.Get<ShutdownSystemOne>() != null,
                    "The unpublished system is disposed inline as Initialize returns; published dependencies survive until the frame boundary.");
            }
            finally { await FinishShutdownFixture(fixture, shutdown); }

            Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2" }) &&
                fixture.Events.SequenceEqual(new[] { "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
                fixture.Instances.Values.All(probe => probe.Disposals == 1),
                "In-flight initialization exits before exactly-once cleanup and no logic initialization follows.");
        }

        private static async UniTask RuntimeRestartStates(TaskSystem system)
        {
            var runtime = LiteRuntime.Instance;
            if (runtime.State != LiteRuntime.RuntimeState.Idle) await runtime.Shutdown();
            Throws<InvalidOperationException>(runtime.Restart, "Idle rejects Restart; startup remains explicit.");

            foreach (var target in new[] { "system 2", "logic 1" })
            {
                var fixture = new ShutdownFixture
                {
                    InitializationGate = new UniTaskCompletionSource<bool>(),
                    GatedInitialization = target
                };
                try
                {
                    await BeginShutdownFixture(fixture);
                    await WaitForRuntimeCondition(() => fixture.Initialized.Contains(target), "Gated initialization: " + target);
                    var expected = target == "system 2" ? LiteRuntime.RuntimeState.InitSystem : LiteRuntime.RuntimeState.InitLogic;
                    Check(runtime.State == expected, "The real startup is held in " + expected + ".");
                    Throws<InvalidOperationException>(runtime.Restart, expected + " rejects Restart.");
                    LiteRuntime.Setting.Common.AutoRestartInBackground = true;
                    LiteRuntime.Setting.Common.BackgroundLimitTime = 0f;
                    runtime.OnEnterBackground();
                    runtime.OnEnterForeground();
                    runtime.Tick(0f);
                    Check(runtime.State == expected && runtime.IsFocus && !runtime.IsPause && fixture.Events.Count == 0,
                        "An overdue foreground return resumes " + expected + " without requesting Restart.");
                    LiteRuntime.Timer.NextFrame(() => fixture.InitializationGate.TrySetResult(true));
                    runtime.Tick(0f);
                    var published = LiteRuntime.Get<ShutdownSystemTwo>();
                    if (target == "system 2")
                    {
                        Check(published != null && published.Ticks == 0,
                            "Initialization resumed by Timer publishes the system immediately without ticking it in the current traversal.");
                    }
                    await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Rejected request startup completion");
                    runtime.Tick(0f);
                    Check(published.Ticks > 0 && runtime.State == LiteRuntime.RuntimeState.Running && fixture.Events.Count == 0,
                        "The published system is updated on the next Tick. A rejected initialization request does not turn into a delayed restart.");
                }
                finally { await FinishShutdownFixture(fixture); }
            }

            var queued = new ShutdownFixture();
            try
            {
                await BeginShutdownFixture(queued);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Queued request startup");
                runtime.Restart();
                runtime.EnterErrorState(FrameworkErrorCode.Unknown);
                Throws<InvalidOperationException>(runtime.Restart, "Error rejects Restart.");
                LiteRuntime.Setting.Common.AutoRestartInBackground = true;
                LiteRuntime.Setting.Common.BackgroundLimitTime = 0f;
                runtime.OnEnterBackground();
                runtime.OnEnterForeground();
                runtime.Tick(0f);
                runtime.Tick(0f);
                Check(runtime.State == LiteRuntime.RuntimeState.Error && queued.Events.Count == 0,
                    "A request accepted before Error is consumed without starting shutdown or another generation.");
            }
            finally { await FinishShutdownFixture(queued); }

            var failed = new ShutdownFixture { FailedInitialization = "system 2" };
            var rejectedInCallback = false;
            void OnStartupError(FrameworkErrorCode code, string message)
            {
                if (code != FrameworkErrorCode.Startup) return;
                try { runtime.Restart(); }
                catch (InvalidOperationException) { rejectedInCallback = true; throw; }
            }
            LiteRuntime.OnFrameworkError += OnStartupError;
            try
            {
                await BeginShutdownFixture(failed);
                await WaitForRuntimeCondition(() => failed.Events.Contains("system 2 start"), "Failed unpublished system starts disposal");
                Check(!rejectedInCallback && LiteRuntime.Get<ShutdownSystemOne>() != null && LiteRuntime.Get<ShutdownSystemTwo>() == null,
                    "The center owns the failed module until its disposal finishes, before Runtime reports the startup failure.");
                failed.SystemGate.TrySetResult();
                await WaitForRuntimeCondition(() => rejectedInCallback, "Startup error callback after Asset manifest collection");
                Check(rejectedInCallback && runtime.State == LiteRuntime.RuntimeState.ShuttingDown,
                    "Restart is explicitly rejected inside the startup error callback while automatic cleanup still begins.");
                Check(_current.evidence.Any(line => line.Contains("Cannot restart runtime in state Error.")),
                    "The original subscriber exception is logged during startup rollback.");
            }
            finally
            {
                LiteRuntime.OnFrameworkError -= OnStartupError;
                await FinishShutdownFixture(failed);
            }
            Check(runtime.State == LiteRuntime.RuntimeState.Idle && failed.Instances.Values.All(probe => probe.Disposals == 1),
                "The throwing error subscriber cannot prevent exactly-once rollback of the unpublished and published systems.");

            var closing = new ShutdownFixture();
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(closing);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Explicit shutdown startup");
                runtime.Restart();
                shutdown = runtime.Shutdown();
                runtime.Restart();
                runtime.Tick(0f);
                Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Explicit Shutdown clears a queued restart and ignores requests received while closing.");
            }
            finally { await FinishShutdownFixture(closing, shutdown); }
            runtime.Tick(0f);
            Check(runtime.State == LiteRuntime.RuntimeState.Idle,
                "Ordinary Shutdown stays Idle instead of being upgraded to Restart.");

            var quitting = new ShutdownFixture();
            var replacement = new ShutdownFixture();
            quitting.LogicGate.TrySetResult();
            try
            {
                await BeginShutdownFixture(quitting);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Quit during restart setup");
                _shutdownFixture = replacement;
                runtime.Restart();
                await WaitForRuntimeCondition(() => quitting.Events.Contains("system 2 start"), "Restart waits for system disposal");
                runtime.OnApplicationQuit();
                quitting.SystemGate.TrySetResult();
                await WaitForRuntimeCondition(() => runtime.State != LiteRuntime.RuntimeState.ShuttingDown, "Quit finishes pending shutdown");
                Check(runtime.State == LiteRuntime.RuntimeState.Idle && replacement.Instances.Count == 0 &&
                    quitting.Instances.Values.All(probe => probe.Disposals == 1),
                    "Quit during restart disposes the old generation once and suppresses replacement startup.");
            }
            finally
            {
                quitting.SystemGate.TrySetResult();
                await FinishShutdownFixture(replacement);
            }
        }

        private static async UniTask RuntimeRestartRequests(TaskSystem system)
        {
            var runtime = LiteRuntime.Instance;
            var previous = new ShutdownFixture();
            var replacement = new ShutdownFixture();
            replacement.LogicGate.TrySetResult();
            replacement.SystemGate.TrySetResult();
            var finalGeneration = replacement;
            try
            {
                await BeginShutdownFixture(previous);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Restart request startup");
                var oldSystem = LiteRuntime.Get<ShutdownSystemTwo>();
                _shutdownFixture = replacement;
                runtime.Restart();
                runtime.Restart();
                runtime.Tick(0f);
                await WaitForRuntimeCondition(() => previous.Events.Contains("logic 1 start"), "Restart logic gate");
                Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown && replacement.Instances.Count == 0,
                    "Repeated Running requests start one shutdown and do not initialize replacements before disposal finishes.");

                runtime.IsPause = true;
                LiteRuntime.Setting.Common.TimeScale = 0f;
                var timerFired = false;
                LiteRuntime.Timer.NextFrame(() => { timerFired = true; previous.LogicGate.TrySetResult(); });
                runtime.Restart();
                runtime.Restart();
                runtime.Tick(0f);
                Check(timerFired && previous.Events.Contains("logic 1 end"),
                    "Ignored closing requests do not skip the Tick that completes cleanup, even while paused.");
                await WaitForRuntimeCondition(() => previous.Events.Contains("system 2 start"), "Restart system gate");
                runtime.Restart();
                LiteRuntime.Timer.NextFrame(() => previous.SystemGate.TrySetResult());
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Replacement startup");
                for (var i = 0; i < 3; i++) { runtime.Tick(0f); await UniTask.NextFrame(); }
                Check(previous.Instances.Values.All(probe => probe.Disposals == 1) && replacement.Instances.Count == 4 &&
                    replacement.Events.Count == 0 && !ReferenceEquals(oldSystem, LiteRuntime.Get<ShutdownSystemTwo>()),
                    "Exactly one replacement generation remains Running with no second queued shutdown.");

                var foregroundReplacement = new ShutdownFixture();
                foregroundReplacement.LogicGate.TrySetResult();
                foregroundReplacement.SystemGate.TrySetResult();
                finalGeneration = foregroundReplacement;
                _shutdownFixture = foregroundReplacement;
                var foregroundSystem = LiteRuntime.Get<ShutdownSystemTwo>();
                LiteRuntime.Setting.Common.AutoRestartInBackground = true;
                LiteRuntime.Setting.Common.BackgroundLimitTime = 0f;
                runtime.OnEnterBackground();
                runtime.OnEnterForeground();
                runtime.Tick(0f);
                Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown,
                    "An overdue foreground return while Running requests the normal restart path.");
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Foreground replacement startup");
                Check(!ReferenceEquals(foregroundSystem, LiteRuntime.Get<ShutdownSystemTwo>()) &&
                    replacement.Instances.Values.All(probe => probe.Disposals == 1) && foregroundReplacement.Instances.Count == 4,
                    "Foreground restart disposes the previous generation once and initializes a working replacement.");

                void FailOnForeground(EnterForegroundEvent message) => runtime.EnterErrorState(FrameworkErrorCode.Unknown);
                LiteRuntime.Event.Register<EnterForegroundEvent>(FailOnForeground);
                try
                {
                    runtime.OnEnterBackground();
                    runtime.OnEnterForeground();
                    runtime.Tick(0f);
                    Check(runtime.State == LiteRuntime.RuntimeState.Error && foregroundReplacement.Events.Count == 0,
                        "Automatic restart uses the state after foreground listeners, so an event entering Error does not request restart.");
                }
                finally { LiteRuntime.Event.UnRegister<EnterForegroundEvent>(FailOnForeground); }
            }
            finally
            {
                previous.LogicGate.TrySetResult();
                previous.SystemGate.TrySetResult();
                await WaitForRuntimeCondition(() => runtime.State != LiteRuntime.RuntimeState.ShuttingDown, "Pending restart completion");
                await FinishShutdownFixture(finalGeneration);
            }
        }

        private static async UniTask RuntimeShutdownFailureStopsTick(TaskSystem system)
        {
            var runtime = LiteRuntime.Instance;
            var fixture = new ShutdownFixture { FailedDisposal = "system 2" };
            fixture.LogicGate.TrySetResult();
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Partial disposal startup");
                runtime.Tick(0f);
                Check(fixture.Instances.Values.All(probe => probe.Ticks > 0),
                    "The probes participate in real Runtime updates before shutdown.");
                var shutdown = runtime.Shutdown();
                await WaitForRuntimeCondition(() => fixture.Events.Contains("system 2 start"), "Partial disposal waiting for Timer");
                var ticksBeforeFailure = fixture.Instances.Values.Sum(probe => probe.Ticks);
                var failureObservedInTimer = false;
                LiteRuntime.Timer.NextFrame(() =>
                {
                    fixture.SystemGate.TrySetResult();
                    failureObservedInTimer = runtime.State == LiteRuntime.RuntimeState.ShutdownFailed;
                });
                runtime.Tick(1f);
                Exception error = null;
                try { await shutdown; } catch (Exception ex) { error = ex; }
                Check(failureObservedInTimer && fixture.Instances.Values.Sum(probe => probe.Ticks) == ticksBeforeFailure &&
                    fixture.TicksAfterDispose == 0,
                    "Timer resumes the failing Dispose inside Runtime.Tick; remaining systems are skipped in that same traversal.");
                Check(ReferenceEquals(error, fixture.DisposalError) && runtime.State == LiteRuntime.RuntimeState.ShutdownFailed &&
                    fixture.Events.Contains("system 2 start") && !fixture.Events.Contains("system 2 end"),
                    "The original partial-system disposal error propagates and leaves Runtime in ShutdownFailed.");
                var ticks = fixture.Instances.Values.Sum(probe => probe.Ticks);
                Throws<InvalidOperationException>(runtime.Restart, "ShutdownFailed rejects Restart.");
                Throws<InvalidOperationException>(() => runtime.Startup(runtime.Launcher, LiteRuntime.Setting), "ShutdownFailed rejects Startup.");
                Exception retryError = null;
                try { await runtime.Shutdown(); } catch (InvalidOperationException ex) { retryError = ex; }
                Check(retryError != null, "A failed shutdown cannot be retried.");
                var timerFired = false;
                LiteRuntime.Timer.NextFrame(() => timerFired = true);
                foreach (var paused in new[] { false, true, false })
                {
                    runtime.IsPause = paused;
                    runtime.Tick(1f);
                    await UniTask.NextFrame();
                }
                Check(!timerFired && fixture.Instances.Values.Sum(probe => probe.Ticks) == ticks && fixture.TicksAfterDispose == 0 &&
                    runtime.State == LiteRuntime.RuntimeState.ShutdownFailed && fixture.Instances["system 2"].Disposals == 1 && fixture.Instances["system 1"].Disposals == 0,
                    "Terminal Runtime ticks advance neither Timer nor any probe, retry disposal, or start a new generation.");
            }
            finally
            {
                if (runtime.State == LiteRuntime.RuntimeState.ShutdownFailed)
                {
                    // Final case in an isolated process; preserve the terminal Runtime through reporting.
                    _shutdownFixture = null;
                }
                else { await FinishShutdownFixture(fixture); }
            }
        }
    }
}
