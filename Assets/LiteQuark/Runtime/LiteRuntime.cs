using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed partial class LiteRuntime : Singleton<LiteRuntime>
    {
        internal enum RuntimeState : byte
        {
            Idle,
            InitSystem,
            InitGameEntry,
            Running,
            ShuttingDown,
            ShutdownFailed,
            Error,
        }

        public static event System.Action<FrameworkErrorCode, string> OnFrameworkError;

        public static bool IsDebugMode { get; private set; } = false;

        public bool IsPause { get; set; }
        public bool IsFocus { get; private set; }

        public LiteLauncher Launcher { get; private set; }

        private LiteSetting _setting = null;
        private IGameEntry _gameEntry;
        private float _enterBackgroundTime = 0.0f;
        private bool _restartWhenNextFrame = false;
        private bool _quitRequested;
        private CancellationTokenSource _startupCts = null;
        private UniTask<bool> _initialization;

        private RuntimeState _state = RuntimeState.Idle;
        internal RuntimeState State => _state;

        private LiteRuntime()
        {
        }

        public void Startup(LiteLauncher launcher, LiteSetting setting)
        {
            if (_state != RuntimeState.Idle)
            {
                throw new System.InvalidOperationException($"Cannot startup runtime in state {_state}.");
            }

            IsPause = false;
            IsFocus = true;
            Launcher = launcher;
            _setting = setting;
            IsDebugMode = Debug.isDebugBuild && _setting.Common.DebugMode;
            _enterBackgroundTime = 0.0f;
            _restartWhenNextFrame = false;
            _quitRequested = false;

            Application.targetFrameRate = _setting.Common.TargetFrameRate;
            Input.multiTouchEnabled = _setting.Common.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int)System.DateTime.Now.Ticks);

            _startupCts = new CancellationTokenSource();
            _state = RuntimeState.InitSystem;

            var ct = _startupCts.Token;
            // Startup and an external shutdown can both await the same initialization.
            _initialization = InitializeAsync(ct).ToAsyncLazy().Task;
            CompleteStartupAsync(ct).Forget();
        }

        private async UniTask<bool> InitializeAsync(CancellationToken ct)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (!await SystemCenter.Instance.InitializeSystem(ct) || ct.IsCancellationRequested)
            {
                return false;
            }
            LLog.Info("Runtime: InitSystem duration {0}s", watch.Elapsed.TotalSeconds);

            IGameEntry entry = null;
            try
            {
                Log.OpenViewer();
                if (ct.IsCancellationRequested)
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(_setting.GameEntry))
                {
                    return true;
                }

                _state = RuntimeState.InitGameEntry;
                var type = System.Type.GetType(_setting.GameEntry, throwOnError: true);
                if (!typeof(IGameEntry).IsAssignableFrom(type))
                {
                    throw new System.InvalidOperationException($"Game entry {type} must implement {nameof(IGameEntry)}.");
                }

                entry = (IGameEntry)System.Activator.CreateInstance(type);
                var initialized = await entry.Initialize();
                if (ct.IsCancellationRequested)
                {
                    return false;
                }
                if (!initialized)
                {
                    LLog.Error("Initialize game entry {0} failed", type.FullName);
                    return false;
                }

                _gameEntry = entry;
                entry = null;
                return true;
            }
            catch (System.OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return false;
            }
            catch (System.Exception ex)
            {
                LLog.Exception(ex);
                return false;
            }
            finally
            {
                if (entry != null)
                {
                    await entry.DisposeAsync();
                }
            }
        }

        private async UniTask CompleteStartupAsync(CancellationToken ct)
        {
            bool ready;
            try
            {
                ready = await _initialization;
            }
            catch (System.Exception ex)
            {
                // An external shutdown observes the original disposal exception itself.
                if (!ct.IsCancellationRequested)
                {
                    _state = RuntimeState.ShutdownFailed;
                    LLog.Exception(ex);
                }
                return;
            }

            _initialization = default;
            if (ct.IsCancellationRequested)
            {
                return;
            }
            if (ready)
            {
                _state = RuntimeState.Running;
                return;
            }

            LiteUtils.SafeInvoke(EnterErrorState, FrameworkErrorCode.Startup);
            if (!ct.IsCancellationRequested)
            {
                await Shutdown();
            }
        }

        public UniTask Shutdown() => Shutdown(false);

        internal void OnApplicationQuit()
        {
            _quitRequested = true;
            if (_state is not RuntimeState.ShuttingDown and not RuntimeState.ShutdownFailed)
            {
                Shutdown(true).Forget();
            }
        }

        private async UniTask Shutdown(bool atFrameBoundary)
        {
            if (_state == RuntimeState.Idle)
            {
                return;
            }

            if (_state is RuntimeState.ShuttingDown or RuntimeState.ShutdownFailed)
            {
                throw new System.InvalidOperationException($"Cannot shutdown runtime in state {_state}.");
            }

            _state = RuntimeState.ShuttingDown;
            _restartWhenNextFrame = false;

            try
            {
                _startupCts?.Cancel();

                if (!atFrameBoundary)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                }
                await FinishAtFrameBoundary(_initialization.AsUniTask());

                if (_gameEntry != null)
                {
                    await FinishAtFrameBoundary(_gameEntry.DisposeAsync());
                    _gameEntry = null;
                }
                await SystemCenter.Instance.DisposeAsync();

                _startupCts?.Dispose();
                _startupCts = null;

                _state = RuntimeState.Idle;
            }
            catch
            {
                _state = RuntimeState.ShutdownFailed;
                throw;
            }
        }

        internal void EnterErrorState(FrameworkErrorCode code)
        {
            if (_state is not RuntimeState.ShuttingDown and not RuntimeState.ShutdownFailed)
            {
                _state = RuntimeState.Error;
                LLog.Error("Enter <ErrorState>, please check log.");
            }

            FrameworkError(code, "Framework error");
        }

        public void Tick(float deltaTime)
        {
            if (_state is RuntimeState.Idle or RuntimeState.Error or RuntimeState.ShutdownFailed)
            {
                return;
            }

            if (_restartWhenNextFrame)
            {
                _restartWhenNextFrame = false;

                if (_state == RuntimeState.Running)
                {
                    RestartRuntime().Forget();
                    return;
                }
            }

            var isShuttingDown = _state == RuntimeState.ShuttingDown;

            if (IsPause && !isShuttingDown)
            {
                return;
            }

            LiteTime.UnscaledDeltaTime = Time.unscaledDeltaTime;

#if UNITY_EDITOR
            var tickDeltaTime = deltaTime * _setting.Common.TimeScale;
#else
            var tickDeltaTime = deltaTime;
#endif

            if (isShuttingDown)
            {
                tickDeltaTime = LiteTime.UnscaledDeltaTime;
            }

            LiteTime.DeltaTime = tickDeltaTime;

            SystemCenter.Instance.Tick(tickDeltaTime);
            if (_state == RuntimeState.Running && _gameEntry is ITick tick)
            {
                tick.Tick(tickDeltaTime);
            }
        }

        internal static async UniTask FinishAtFrameBoundary(UniTask task)
        {
            var pending = task.Status == UniTaskStatus.Pending;
            await task;
            if (pending)
            {
                // A Timer or Task tick may have resumed this continuation inline.
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }
        }

        public void Restart()
        {
            if (_state == RuntimeState.ShuttingDown)
            {
                return;
            }

            if (_state != RuntimeState.Running)
            {
                throw new System.InvalidOperationException($"Cannot restart runtime in state {_state}.");
            }

            _restartWhenNextFrame = true;
        }

        private async UniTask RestartRuntime()
        {
            Debug.ClearDeveloperConsole();
            await Shutdown(true);
            if (!_quitRequested)
            {
                Startup(Launcher, _setting);
            }
        }

        public void OnEnterForeground()
        {
            if (IsFocus)
            {
                return;
            }

            IsPause = false;
            IsFocus = true;

            LLog.Info("OnEnterForeground");
            Event?.Send<EnterForegroundEvent>();

            if (_state == RuntimeState.Running && _setting.Common.AutoRestartInBackground &&
                Time.realtimeSinceStartup - _enterBackgroundTime >= _setting.Common.BackgroundLimitTime)
            {
                Restart();
                return;
            }

            _enterBackgroundTime = Time.realtimeSinceStartup;
        }

        public void OnEnterBackground()
        {
            if (!IsFocus)
            {
                return;
            }

            IsPause = true;
            IsFocus = false;

            LLog.Info("OnEnterBackground");
            Event?.Send<EnterBackgroundEvent>();
            _enterBackgroundTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Get System
        /// </summary>
        public static T Get<T>() where T : ISystem
        {
            return SystemCenter.Instance.GetSystem<T>();
        }

        internal static void FrameworkError(FrameworkErrorCode code, string message)
        {
            LLog.Error("FrameworkError: {0}, {1}", code, message);
            OnFrameworkError?.Invoke(code, message);
        }

        public static LiteSetting Setting => Instance._setting;

        // frequently used system
        public static LogSystem Log => Get<LogSystem>();
        public static EventSystem Event => Get<EventSystem>();
        public static TaskSystem Task => Get<TaskSystem>();
        public static TimerSystem Timer => Get<TimerSystem>();
        public static AssetSystem Asset => Get<AssetSystem>();
        public static ObjectPoolSystem ObjectPool => Get<ObjectPoolSystem>();
        public static ActionSystem Action => Get<ActionSystem>();
    }
}
