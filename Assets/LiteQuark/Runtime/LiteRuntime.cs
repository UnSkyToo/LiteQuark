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
            InitLogic,
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
        private float _enterBackgroundTime = 0.0f;
        private bool _restartWhenNextFrame = false;
        private CancellationTokenSource _startupCts = null;
        private UniTaskCompletionSource _initializationExited;
        private bool _isTicking;

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

            Application.targetFrameRate = _setting.Common.TargetFrameRate;
            Input.multiTouchEnabled = _setting.Common.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int)System.DateTime.Now.Ticks);

            _initializationExited = new UniTaskCompletionSource();
            _startupCts = new CancellationTokenSource();
            _state = RuntimeState.InitSystem;

            StartupAsync(_startupCts.Token).Forget();
        }

        private async UniTask StartupAsync(CancellationToken ct)
        {
            var startupFailed = false;

            try
            {
                var watch = new System.Diagnostics.Stopwatch();

                _state = RuntimeState.InitSystem;
                watch.Restart();
                if (!await SystemCenter.Instance.InitializeSystem(ct))
                {
                    if (!ct.IsCancellationRequested)
                    {
                        startupFailed = true;
                        EnterErrorState(FrameworkErrorCode.Startup);
                    }

                    return;
                }

                LLog.Info("Runtime: InitSystem duration {0}s", watch.Elapsed.TotalSeconds);

                if (ct.IsCancellationRequested)
                {
                    return;
                }

                _state = RuntimeState.InitLogic;
                watch.Restart();
                if (!await LogicCenter.Instance.InitializeLogic(ct))
                {
                    if (!ct.IsCancellationRequested)
                    {
                        startupFailed = true;
                        EnterErrorState(FrameworkErrorCode.Startup);
                    }

                    return;
                }

                LLog.Info("Runtime: InitLogic duration {0}s", watch.Elapsed.TotalSeconds);

                if (ct.IsCancellationRequested)
                {
                    return;
                }

                _state = RuntimeState.Running;
            }
            catch (System.OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutdown triggered during startup, silently exit
            }
            catch (System.Exception ex)
            {
                startupFailed = true;

                LLog.Exception(ex);
                EnterErrorState(FrameworkErrorCode.Startup);
            }
            finally
            {
                _initializationExited.TrySetResult();

                if (startupFailed && _state is not RuntimeState.ShuttingDown and not RuntimeState.ShutdownFailed)
                {
                    await Shutdown();
                }
            }
        }

        public async UniTask Shutdown()
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

                if (_initializationExited != null)
                {
                    await _initializationExited.Task;
                }

                await UniTask.NextFrame();

                await LogicCenter.Instance.DisposeAsync();
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
            if (_restartWhenNextFrame)
            {
                RestartRuntime().Forget();
                return;
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

            _isTicking = true;
            try
            {
                SystemCenter.Instance.Tick(tickDeltaTime);

                if (_state == RuntimeState.Running)
                {
                    LogicCenter.Instance.Tick(tickDeltaTime);
                }
            }
            finally
            {
                _isTicking = false;
            }
        }

        internal async UniTask WaitForTickExitAsync()
        {
            while (_isTicking)
            {
                await UniTask.NextFrame();
            }
        }

        public void Restart()
        {
            _restartWhenNextFrame = true;
        }

        private async UniTask RestartRuntime()
        {
            _restartWhenNextFrame = false;
            Debug.ClearDeveloperConsole();
            await Shutdown();
            Startup(Launcher, _setting);
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

            if (_setting.Common.AutoRestartInBackground && Time.realtimeSinceStartup - _enterBackgroundTime >= _setting.Common.BackgroundLimitTime)
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
