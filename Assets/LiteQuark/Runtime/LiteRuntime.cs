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
            Error,
        }

        public static event System.Action<FrameworkErrorCode, string> OnFrameworkError;

        public static bool IsDebugMode => Setting?.Environment == LiteBuildProfile.Development;

        public bool IsPause { get; set; }
        public bool IsFocus { get; private set; }
        public bool CanLateTick => _state == RuntimeState.Running && !IsPause;

        public LiteLauncher Launcher { get; private set; }

        private LiteSetting _setting = null;
        private bool _useLauncherSettings;
        private IGameEntry _gameEntry;
        private int _lateTickFrame = -1;
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

        public void Startup(LiteLauncher launcher, LiteSetting setting = null)
        {
            if (_state != RuntimeState.Idle)
            {
                throw new System.InvalidOperationException($"Cannot startup runtime in state {_state}.");
            }

            IsPause = false;
            IsFocus = true;
            Launcher = launcher;
            _lateTickFrame = -1;
            _useLauncherSettings = setting == null;
            _setting = setting ?? launcher.CreateRuntimeSetting();
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
            IGameEntry entry = null;
            try
            {
                if (!await SystemCenter.Instance.InitializeSystem(ct) || ct.IsCancellationRequested)
                {
                    return false;
                }
                LLog.Info("Runtime: InitSystem duration {0}s", watch.Elapsed.TotalSeconds);
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
                var initialized = await entry.Initialize(ct);
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
                    await TryDisposeAsync(entry);
                }
            }
        }

        private async UniTask CompleteStartupAsync(CancellationToken ct)
        {
            var ready = await _initialization;
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

        public UniTask Shutdown() => Shutdown(false).AsUniTask();

        internal void OnApplicationQuit()
        {
            _quitRequested = true;
            if (_state != RuntimeState.ShuttingDown)
            {
                Shutdown(true).Forget();
            }
        }

        private async UniTask<bool> Shutdown(bool atFrameBoundary)
        {
            if (_state == RuntimeState.Idle)
            {
                return true;
            }

            if (_state == RuntimeState.ShuttingDown)
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
                await FinishAtFrameBoundary(_initialization);

                var success = true;
                if (_gameEntry != null)
                {
                    success = await FinishAtFrameBoundary(TryDisposeAsync(_gameEntry));
                    _gameEntry = null;
                }
                success &= await SystemCenter.Instance.DisposeAsync();
                return success;
            }
            finally
            {
                _startupCts?.Dispose();
                _startupCts = null;
                _state = RuntimeState.Idle;
            }
        }

        internal void EnterErrorState(FrameworkErrorCode code)
        {
            if (_state != RuntimeState.ShuttingDown)
            {
                _state = RuntimeState.Error;
                LLog.Error("Enter <ErrorState>, please check log.");
            }

            FrameworkError(code, "Framework error");
        }

        public void Tick(float deltaTime)
        {
            _lateTickFrame = -1;
            if (_state is RuntimeState.Idle or RuntimeState.Error)
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
            if (CanLateTick)
            {
                _lateTickFrame = Time.frameCount;
            }
        }

        public void LateTick()
        {
            if (_lateTickFrame != Time.frameCount)
            {
                return;
            }
            _lateTickFrame = -1;
            if (!CanLateTick)
            {
                return;
            }

            var deltaTime = LiteTime.DeltaTime;
            if (_gameEntry is ILateTick entry)
            {
                entry.LateTick(deltaTime);
            }
            SystemCenter.Instance.LateTick(deltaTime);
        }

        internal static async UniTask<bool> TryDisposeAsync(IDisposeAsync target)
        {
            try
            {
                await target.DisposeAsync();
                return true;
            }
            catch (System.Exception ex)
            {
                // LogSystem 自身也可能正在释放。
                Debug.LogException(ex);
                return false;
            }
        }

        internal static async UniTask<bool> FinishAtFrameBoundary(UniTask<bool> task)
        {
            var pending = task.Status == UniTaskStatus.Pending;
            var result = await task;
            if (pending)
            {
                // A Timer or Task tick may have resumed this continuation inline.
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }
            return result;
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
            if (await Shutdown(true) && !_quitRequested)
            {
                Startup(Launcher, _useLauncherSettings ? null : _setting);
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
