using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            internal readonly UniTaskCompletionSource EntryGate = new();
            internal readonly UniTaskCompletionSource SystemGate = new();
            internal UniTaskCompletionSource<bool> InitializationGate;
            internal string GatedInitialization = "system 2";
            internal Type EntryType = typeof(ShutdownEntry);
            internal string FailedInitialization;
            internal string FailedDisposal;
            internal bool ThrowOnInitialize;
            internal bool InTimerCallback;
            internal bool DisposalStartedInTimerCallback;
            internal int TicksAfterDispose;
            internal Action<string, float> OnLateTick;
        }

        private static ShutdownFixture _shutdownFixture;

        private abstract class ShutdownProbe
        {
            private readonly string _name;
            private readonly ShutdownFixture _fixture;
            private bool _disposed;
            internal int Disposals;
            internal int Ticks;
            internal CancellationToken StartupToken;

            protected ShutdownProbe(string name)
            {
                _name = name;
                _fixture = _shutdownFixture;
                _fixture.Instances.Add(name, this);
            }

            public async UniTask<bool> Initialize(CancellationToken ct = default)
            {
                StartupToken = ct;
                _fixture.Initialized.Add(_name);
                if (_name == _fixture.FailedInitialization)
                {
                    if (_fixture.ThrowOnInitialize)
                        throw new InvalidOperationException("InjectedValidation runtime initialization");
                    return false;
                }
                if (_name == _fixture.GatedInitialization && _fixture.InitializationGate != null)
                    return await _fixture.InitializationGate.Task.AttachExternalCancellation(ct);
                return true;
            }

            public void Tick(float deltaTime)
            {
                Ticks++;
                if (_disposed) _fixture.TicksAfterDispose++;
            }

            public void LateTick(float deltaTime) => _fixture.OnLateTick?.Invoke(_name, deltaTime);

            public async UniTask DisposeAsync()
            {
                Disposals++;
                _fixture.DisposalStartedInTimerCallback |= _fixture.InTimerCallback;
                _fixture.Events.Add(_name + " start");
                if (_name == "entry") await _fixture.EntryGate.Task;
                if (_name == "system 2") await _fixture.SystemGate.Task;
                _disposed = true;
                if (_name == _fixture.FailedDisposal) throw new InvalidOperationException("InjectedValidation partial system disposal: " + _name);
                _fixture.Events.Add(_name + " end");
            }
        }

        private sealed class ShutdownSystemOne : ShutdownProbe, ISystem, ITick, ILateTick
        {
            public ShutdownSystemOne() : base("system 1") { }
        }

        private sealed class ShutdownSystemTwo : ShutdownProbe, ISystem, ITick, ILateTick
        {
            public ShutdownSystemTwo() : base("system 2") { }
        }

        private sealed class ShutdownEntry : ShutdownProbe, IGameEntry, ITick
        {
            public ShutdownEntry() : base("entry") { }
        }

        private sealed class EntryWithoutTick : ShutdownProbe, IGameEntry, ILateTick
        {
            public EntryWithoutTick() : base("entry") { }
        }

        private sealed class SystemWithoutTick : ShutdownProbe, ISystem, ILateTick
        {
            public SystemWithoutTick() : base("system 1") { }
        }

        private static async UniTask BeginShutdownFixture(ShutdownFixture fixture, Type firstSystemType = null)
        {
            var runtime = LiteRuntime.Instance;
            if (runtime.State != LiteRuntime.RuntimeState.Idle) await runtime.Shutdown();

            _shutdownFixture = fixture;
            var setting = new LiteSetting();
            setting.SystemList.Add(new LiteSystemEntry { AssemblyQualifiedName = (firstSystemType ?? typeof(ShutdownSystemOne)).AssemblyQualifiedName });
            setting.SystemList.Add(new LiteSystemEntry { AssemblyQualifiedName = typeof(ShutdownSystemTwo).AssemblyQualifiedName });
            setting.GameEntry = fixture.EntryType.AssemblyQualifiedName;
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
                fixture.EntryGate.TrySetResult();
                fixture.SystemGate.TrySetResult();

                var runtime = LiteRuntime.Instance;
                if (!shutdown.HasValue && runtime.State is not LiteRuntime.RuntimeState.Idle and not LiteRuntime.RuntimeState.ShuttingDown)
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

        [Serializable]
        private sealed class TimeoutSetting : ISystemSetting
        {
            public BuildValue<int> Timeout;
        }

        private class EndpointProbe : ISystem, ISystemSettingProvider<TimeoutSetting>
        {
            public TimeoutSetting Setting { get; set; }
            public int Timeout { get; private set; }

            public UniTask<bool> Initialize(CancellationToken ct = default)
            {
                Timeout = Setting.Timeout.Resolve(LiteRuntime.Setting.Environment);
                return UniTask.FromResult(true);
            }

            public UniTask DisposeAsync() => UniTask.CompletedTask;
        }

        private sealed class OtherEndpointProbe : EndpointProbe { }

        private sealed class FailingCloseAppender : ILogAppender
        {
            public string Name { get; set; }
            public void Open() { }
            public void DoAppend(LoggingEvent logEvent) { }
            public void Close() => throw new InvalidOperationException(nameof(FailingCloseAppender));
        }

        private static async UniTask RuntimeSettingsProfiles(TaskSystem system)
        {
            var runtime = LiteRuntime.Instance;
            var launcher = runtime.Launcher;
            var originalSettings = launcher.Settings;
            var originalPreview = launcher.EditorPreview;
            var authored = new LiteLauncherSettings();
            var startupOrder = new List<string>();
            void RecordStartup(string message, string stack, LogType type)
            {
                const string prefix = "Initialize ";
                if (type == LogType.Log && message.StartsWith(prefix, StringComparison.Ordinal))
                    startupOrder.Add(message.Substring(prefix.Length));
            }
            static ISystem[] RegisteredSystems() => new ISystem[]
            {
                LiteRuntime.Log, LiteRuntime.Event, LiteRuntime.Timer, LiteRuntime.Task, LiteRuntime.Asset,
                LiteRuntime.ObjectPool, LiteRuntime.Action, LiteRuntime.Get<EndpointProbe>(), LiteRuntime.Get<OtherEndpointProbe>()
            };
            Application.logMessageReceived += RecordStartup;
            try
            {
                if (runtime.State != LiteRuntime.RuntimeState.Idle) await runtime.Shutdown();
                launcher.Settings = authored;
                authored.Task.ConcurrencyLimit = 9;
                authored.Asset.BundleModeByBuild = new BuildValue<BundleLoadMode>(BundleLoadMode.Simulate);
                authored.Asset.BundleRemoteUriByBuild = new BuildValue<string>("https://release.example/", "https://dev.example/");
                authored.Log.LevelsByBuild = new BuildValue<LogLevel>(LogLevel.Warn, LogLevel.All);
                var ui = new UISystemSetting { ResolutionWidth = 1600 };
                authored.SystemList.Add(new LiteSystemEntry { Disabled = true, AssemblyQualifiedName = typeof(UISystem).AssemblyQualifiedName, Setting = ui });
                authored.SystemList.Add(new LiteSystemEntry
                {
                    AssemblyQualifiedName = typeof(EndpointProbe).AssemblyQualifiedName,
                    Setting = new TimeoutSetting { Timeout = new BuildValue<int>(15, 0) }
                });
                authored.SystemList.Add(new LiteSystemEntry
                {
                    AssemblyQualifiedName = typeof(OtherEndpointProbe).AssemblyQualifiedName,
                    Setting = new TimeoutSetting { Timeout = new BuildValue<int>(60) }
                });
                authored.SystemList.Add(new LiteSystemEntry { AssemblyQualifiedName = typeof(TaskSystem).AssemblyQualifiedName });
                authored.SystemList.Add(new LiteSystemEntry { AssemblyQualifiedName = typeof(EndpointProbe).AssemblyQualifiedName });
                using (var inspector = new SerializedObject(launcher))
                {
                    var timeout = inspector.FindProperty("Settings.SystemList").GetArrayElementAtIndex(1)
                        .FindPropertyRelative(nameof(LiteSystemEntry.Setting)).FindPropertyRelative(nameof(TimeoutSetting.Timeout));
                    Check(BuildValueDrawer.ValueFor(timeout, LiteBuildProfile.Development, out var editable).intValue == 0 && editable,
                        "An enabled Addon override exposes its own Development value in the Inspector.");
                }
                launcher.EditorPreview = LiteBuildProfile.Release;
                var release = launcher.CreateRuntimeSetting();
                Check(release.Asset.BundleRemoteUri == "https://release.example/" && release.Log.Levels == LogLevel.Warn,
                    "Release preview takes the release value even with a Development override.");
                launcher.EditorPreview = LiteBuildProfile.Development;
                runtime.Startup(launcher);
                var development = LiteRuntime.Setting;
                // Asset initialization is pending. The active startup already captured this registration.
                development.SystemList.RemoveAt(2);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Development settings startup");
                var firstSystems = RegisteredSystems();
                Check(firstSystems.All(s => s != null) && startupOrder.SequenceEqual(firstSystems.Select(s => s.GetType().AssemblyQualifiedName)),
                    "Startup creates all seven built-ins in order, then the enabled extensions from the original list, skipping duplicate types.");
                Check(development.Environment == LiteBuildProfile.Development && development.Task.ConcurrencyLimit == 9 &&
                    development.Asset.BundleRemoteUri == "https://dev.example/" && LiteRuntime.Log.GetLogger("Profile").IsInfoEnabled,
                    "Development startup combines shared parameters, its CDN and its log policy.");
                development.Task.ConcurrencyLimit = 1;
                Check(LiteRuntime.Get<UISystem>() == null && LiteRuntime.Get<EndpointProbe>().Timeout == 0 &&
                    LiteRuntime.Get<OtherEndpointProbe>().Timeout == 60,
                    "Systems using the same settings type receive their own row. An explicit zero overrides; an unchecked field inherits.");
                ((UISystemSetting)development.SystemList[0].Setting).ResolutionWidth = 640;
                development.Asset.BundleDownloadRetry.MaxRetries = 0;
                development.Asset.BundleRemoteUri = "https://runtime.example/";

                launcher.EditorPreview = LiteBuildProfile.Release;
                startupOrder.Clear();
                runtime.Restart();
                runtime.Tick(0f);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running &&
                    LiteRuntime.Setting != development, "Fresh Release snapshot after restart");
                var current = LiteRuntime.Setting;
                var log = LiteRuntime.Log.GetLogger("Profile");
                Check(current.Environment == LiteBuildProfile.Release && !LiteRuntime.IsDebugMode && !log.IsInfoEnabled &&
                    log.IsWarnEnabled && !log.IsErrorEnabled && !log.IsFatalEnabled && current.Asset.BundleRemoteUri == "https://release.example/" &&
                    startupOrder.Count == 0,
                    "Release restart applies its CDN and log levels, filtering even the first startup log before LogSystem is published.");
                Check(RegisteredSystems().All(s => s != null && !firstSystems.Contains(s)),
                    "Restart publishes fresh instances of every built-in and enabled extension.");
                Check(LiteRuntime.Get<EndpointProbe>().Timeout == 15 && LiteRuntime.Get<OtherEndpointProbe>().Timeout == 60,
                    "Restart resolves each registration against Release.");
                Check(current.Task.ConcurrencyLimit == 9 && ((UISystemSetting)current.SystemList[0].Setting).ResolutionWidth == 1600 &&
                    current.Asset.BundleDownloadRetry.MaxRetries > 0 && authored.Asset.BundleRemoteUriByBuild.Development == "https://dev.example/" &&
                    ui.ResolutionWidth == 1600,
                    "Runtime changes to profile, shared, nested retry and managed-reference Addon data do not write back to scene settings or leak across restart.");

                ((LoggerBase)log.Logger).AddAppender(new FailingCloseAppender());
                await runtime.Shutdown();
                launcher.EditorPreview = LiteBuildProfile.Development;
                startupOrder.Clear();
                runtime.Startup(launcher);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Startup after appender close failure");
                Check(_current.evidence.Any(line => line.Contains(nameof(FailingCloseAppender))) &&
                    startupOrder.Contains(typeof(EndpointProbe).AssemblyQualifiedName),
                    "After an appender fails to close, LLog emits the new profile's Info messages.");
            }
            finally
            {
                Application.logMessageReceived -= RecordStartup;
                if (runtime.State != LiteRuntime.RuntimeState.Idle) await runtime.Shutdown();
                launcher.Settings = originalSettings;
                launcher.EditorPreview = originalPreview;
            }
        }

        private static async UniTask RuntimeLateTick(TaskSystem system)
        {
            var runtime = LiteRuntime.Instance;
            var calls = new List<(string, float)>();
            var fixture = new ShutdownFixture
            {
                EntryType = typeof(EntryWithoutTick),
                GatedInitialization = "entry",
                InitializationGate = new UniTaskCompletionSource<bool>(),
                OnLateTick = (name, dt) => calls.Add((name, dt))
            };
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(fixture, typeof(SystemWithoutTick));
                await WaitForRuntimeCondition(() => fixture.Initialized.Contains("entry"), "Pending late-tick entry");
                runtime.Tick(1f);
                runtime.LateTick();
                Check(calls.Count == 0, "Published systems receive no late updates while the entry is initializing.");
                fixture.InitializationGate.TrySetResult(true);
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Late-tick startup");

                LiteRuntime.Setting.Common.TimeScale = 0.5f;
                runtime.Tick(0.25f);
                LiteRuntime.Setting.Common.TimeScale = 1f;
                runtime.LateTick();
                runtime.LateTick();
                Check(calls.SequenceEqual(new[] { ("entry", 0.125f), ("system 1", 0.125f), ("system 2", 0.125f) }) &&
                    fixture.Instances["entry"].Ticks == 0 && fixture.Instances["system 1"].Ticks == 0,
                    "Late-only entry and system callbacks run once in entry-first, registration order using the Update delta.");

                calls.Clear();
                runtime.Tick(0f);
                runtime.IsPause = true;
                runtime.LateTick();
                runtime.Tick(1f);
                runtime.IsPause = false;
                runtime.LateTick();
                Check(calls.Count == 0, "Pausing between phases or resuming after a skipped Update dispatches no late updates.");

                fixture.OnLateTick = (name, dt) =>
                {
                    calls.Add((name, dt));
                    if (name == "system 1") shutdown = runtime.Shutdown();
                };
                runtime.Tick(0.25f);
                runtime.LateTick();
                runtime.Tick(0.25f);
                runtime.LateTick();
                Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown &&
                    calls.SequenceEqual(new[] { ("entry", 0.25f), ("system 1", 0.25f) }),
                    "Shutdown inside a late system stops the current traversal and subsequent late updates.");
            }
            finally { await FinishShutdownFixture(fixture, shutdown); }
        }

        private static async UniTask RuntimeShutdownOrder(TaskSystem system)
        {
            var fixture = new ShutdownFixture { EntryType = typeof(EntryWithoutTick) };
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Running, "Probe startup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2", "entry" }) &&
                    LiteRuntime.Get<ShutdownSystemOne>() != null && LiteRuntime.Get<ShutdownSystemTwo>() != null,
                    "Probes initialize through Runtime configuration and publish their systems.");
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Instances["system 2"].Ticks > 0 && fixture.Instances["entry"].Ticks == 0,
                    "An entry without ITick runs its lifecycle while only the systems receive updates.");

                shutdown = LiteRuntime.Instance.Shutdown();
                Check(fixture.Events.Count == 0 && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Shutdown defers disposal until the initialization callback stack has exited.");
                await WaitForRuntimeCondition(() => fixture.Events.Count > 0, "Entry disposal");
                Check(fixture.Events.SequenceEqual(new[] { "entry start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Runtime shutdown waits for asynchronous entry disposal before starting later cleanup.");
                await UniTask.NextFrame();
                Check(fixture.Events.SequenceEqual(new[] { "entry start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "The pending entry continues to hold shutdown across a player loop.");

                LiteRuntime.Instance.EnterErrorState(FrameworkErrorCode.Unknown);
                Check(LiteRuntime.Instance.State == LiteRuntime.RuntimeState.ShuttingDown,
                    "Reporting an error does not overwrite the active shutdown state.");

                LiteRuntime.Instance.IsPause = true;
                LiteRuntime.Setting.Common.TimeScale = 0f;
                LiteRuntime.Timer.NextFrame(() =>
                {
                    fixture.InTimerCallback = true;
                    try { fixture.EntryGate.TrySetResult(); }
                    finally { fixture.InTimerCallback = false; }
                });
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.SequenceEqual(new[] { "entry start", "entry end" }),
                    "Paused shutdown still drives Timer, but does not start the next disposal inside its callback.");

                await WaitForRuntimeCondition(() => fixture.Events.Contains("system 2 start"), "First system disposal");
                Check(fixture.Events.SequenceEqual(new[] { "entry start", "entry end", "system 2 start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "The entry finishes before reverse-order system disposal begins.");
                await UniTask.NextFrame();
                Check(fixture.Events.Count == 3 && shutdown.Value.Status == UniTaskStatus.Pending,
                    "The pending system holds shutdown across a player loop.");

                LiteRuntime.Timer.NextFrame(() =>
                {
                    fixture.InTimerCallback = true;
                    try { fixture.SystemGate.TrySetResult(); }
                    finally { fixture.InTimerCallback = false; }
                });
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.Count == 4 && LiteRuntime.Get<ShutdownSystemTwo>() == null &&
                    !fixture.DisposalStartedInTimerCallback && fixture.TicksAfterDispose == 0,
                    "A disposed system is unpublished and skipped by the remaining Tick before the next system is released.");
            }
            finally
            {
                await FinishShutdownFixture(fixture, shutdown);
            }

            Check(fixture.Events.SequenceEqual(new[] { "entry start", "entry end", "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
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
                    "A false system initialization prevents entry construction.");
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
            var fixture = new ShutdownFixture { FailedInitialization = "entry", ThrowOnInitialize = true };
            fixture.EntryGate.TrySetResult();
            fixture.SystemGate.TrySetResult();
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle, "Throwing initialization cleanup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2", "entry" }),
                    "The throwing entry is created through the real initialization path.");
                Check(fixture.Events.SequenceEqual(new[] { "entry start", "entry end", "system 2 start", "system 2 end", "system 1 start", "system 1 end" }),
                    "The failed entry is cleaned before its systems are disposed in reverse order.");
                Check(fixture.Instances.Values.All(probe => probe.Disposals == 1),
                    "Initialization exceptions do not omit or duplicate disposal.");
            }
            finally { await FinishShutdownFixture(fixture); }
        }

        private static async UniTask RuntimeShutdownDuringInitialization(TaskSystem system)
        {
            await CheckShutdownDuringInitialization(quitting: false);
            await CheckShutdownDuringInitialization(quitting: true);

            var fixture = new ShutdownFixture
            {
                InitializationGate = new UniTaskCompletionSource<bool>(),
                GatedInitialization = "entry"
            };
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => fixture.Initialized.Contains("entry"), "Pending entry initialization");
                shutdown = LiteRuntime.Instance.Shutdown();
                await WaitForRuntimeCondition(() => fixture.Events.Contains("entry start"), "Cancelled entry disposal");
                Check(fixture.Instances["entry"].StartupToken.IsCancellationRequested &&
                    !fixture.InitializationGate.Task.Status.IsCompleted() &&
                    fixture.Events.SequenceEqual(new[] { "entry start" }) && LiteRuntime.Get<ShutdownSystemTwo>() != null,
                    "Shutdown cancels the entry's initialization before disposing it with its systems still available.");
            }
            finally { await FinishShutdownFixture(fixture, shutdown); }
            Check(fixture.Instances["entry"].Ticks == 0 && fixture.Instances.Values.All(probe => probe.Disposals == 1),
                "A cancelled entry never ticks and every initialized object is released once.");
        }

        private static async UniTask CheckShutdownDuringInitialization(bool quitting)
        {
            var fixture = new ShutdownFixture { InitializationGate = new UniTaskCompletionSource<bool>() };
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
                await WaitForRuntimeCondition(() => fixture.Events.Contains("system 2 start"), "Cancelled system disposal");
                Check(fixture.Instances["system 2"].StartupToken.IsCancellationRequested &&
                    !fixture.InitializationGate.Task.Status.IsCompleted() &&
                    fixture.Events.SequenceEqual(new[] { "system 2 start" }) && LiteRuntime.Get<ShutdownSystemTwo>() == null &&
                    LiteRuntime.Get<ShutdownSystemOne>() != null,
                    "Shutdown or quit cancels the unpublished system's initialization while published dependencies stay available for disposal.");
            }
            finally { await FinishShutdownFixture(fixture, shutdown); }

            Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2" }) &&
                fixture.Events.SequenceEqual(new[] { "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
                fixture.Instances.Values.All(probe => probe.Disposals == 1),
                "In-flight initialization exits before exactly-once cleanup and no entry initialization follows.");
        }

        private static async UniTask RuntimeRestartStates(TaskSystem system)
        {
            var runtime = LiteRuntime.Instance;
            if (runtime.State != LiteRuntime.RuntimeState.Idle) await runtime.Shutdown();
            Throws<InvalidOperationException>(runtime.Restart, "Idle rejects Restart; startup remains explicit.");

            foreach (var target in new[] { "system 2", "entry" })
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
                    var expected = target == "system 2" ? LiteRuntime.RuntimeState.InitSystem : LiteRuntime.RuntimeState.InitGameEntry;
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
            quitting.EntryGate.TrySetResult();
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
            replacement.EntryGate.TrySetResult();
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
                await WaitForRuntimeCondition(() => previous.Events.Contains("entry start"), "Restart entry gate");
                Check(runtime.State == LiteRuntime.RuntimeState.ShuttingDown && replacement.Instances.Count == 0,
                    "Repeated Running requests start one shutdown and do not initialize replacements before disposal finishes.");

                runtime.IsPause = true;
                LiteRuntime.Setting.Common.TimeScale = 0f;
                var timerFired = false;
                LiteRuntime.Timer.NextFrame(() => { timerFired = true; previous.EntryGate.TrySetResult(); });
                runtime.Restart();
                runtime.Restart();
                runtime.Tick(0f);
                Check(timerFired && previous.Events.Contains("entry end"),
                    "Ignored closing requests do not skip the Tick that completes cleanup, even while paused.");
                await WaitForRuntimeCondition(() => previous.Events.Contains("system 2 start"), "Restart system gate");
                runtime.Restart();
                LiteRuntime.Timer.NextFrame(() => previous.SystemGate.TrySetResult());
                await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Replacement startup");
                for (var i = 0; i < 3; i++) { runtime.Tick(0f); await UniTask.NextFrame(); }
                Check(previous.Instances.Values.All(probe => probe.Disposals == 1) && replacement.Instances.Count == 3 &&
                    replacement.Events.Count == 0 && !ReferenceEquals(oldSystem, LiteRuntime.Get<ShutdownSystemTwo>()),
                    "Exactly one replacement generation remains Running with no second queued shutdown.");

                var foregroundReplacement = new ShutdownFixture();
                foregroundReplacement.EntryGate.TrySetResult();
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
                    replacement.Instances.Values.All(probe => probe.Disposals == 1) && foregroundReplacement.Instances.Count == 3,
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
                previous.EntryGate.TrySetResult();
                previous.SystemGate.TrySetResult();
                await WaitForRuntimeCondition(() => runtime.State != LiteRuntime.RuntimeState.ShuttingDown, "Pending restart completion");
                await FinishShutdownFixture(finalGeneration);
            }
        }

        private static async UniTask RuntimeShutdownFailure(TaskSystem system)
        {
            var runtime = LiteRuntime.Instance;
            foreach (var failure in new[] { "system 2", "entry" })
            {
                var fixture = new ShutdownFixture { FailedDisposal = failure };
                fixture.EntryGate.TrySetResult();
                fixture.SystemGate.TrySetResult();
                try
                {
                    await BeginShutdownFixture(fixture);
                    await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Startup after prior cleanup");
                    if (failure == "entry")
                    {
                        await runtime.Shutdown();
                    }
                    else
                    {
                        runtime.Restart();
                        await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Idle, "Failed restart finishes cleanup");
                    }

                    Check(_current.evidence.Any(line => line.Contains("InjectedValidation partial system disposal: " + failure)) &&
                        runtime.State == LiteRuntime.RuntimeState.Idle && fixture.Instances.Values.All(probe => probe.Disposals == 1) &&
                        LiteRuntime.Get<ShutdownSystemTwo>() == null && LiteRuntime.Asset == null && LiteRuntime.Task == null,
                        $"A failing {failure} is logged, remaining systems are released, and Runtime returns to Idle.");
                }
                finally { await FinishShutdownFixture(fixture); }
            }
        }
    }
}
