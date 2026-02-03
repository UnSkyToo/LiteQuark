using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed partial class LiteRuntime : Singleton<LiteRuntime>
    {
        private enum RuntimeState : byte
        {
            Idle,
            InitSystem,
            InitLogic,
            Running,
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

        private RuntimeState _state = RuntimeState.Idle;

        private LiteRuntime()
        {
        }

        public void Startup(LiteLauncher launcher)
        {
            IsPause = false;
            IsFocus = true;
            Launcher = launcher;
            _setting = launcher.Setting;
            IsDebugMode = Debug.isDebugBuild && _setting.Common.DebugMode;
            _enterBackgroundTime = 0.0f;
            _restartWhenNextFrame = false;

            Application.targetFrameRate = _setting.Common.TargetFrameRate;
            Input.multiTouchEnabled = _setting.Common.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int)System.DateTime.Now.Ticks);

            StartupAsync();
        }

        private async void StartupAsync()
        {
            var watch = new System.Diagnostics.Stopwatch();

            _state = RuntimeState.InitSystem;
            watch.Restart();
            if (!await SystemCenter.Instance.InitializeSystem())
            {
                EnterErrorState();
                return;
            }

            LLog.Info("Runtime: InitSystem duration {0}s", watch.Elapsed.TotalSeconds);

            _state = RuntimeState.InitLogic;
            watch.Restart();
            if (!await LogicCenter.Instance.InitializeLogic())
            {
                EnterErrorState();
                return;
            }

            LLog.Info("Runtime: InitLogic duration {0}s", watch.Elapsed.TotalSeconds);

            _state = RuntimeState.Running;
        }

        public void Shutdown()
        {
            OnEnterBackground();

            if (_state == RuntimeState.Running)
            {
                LogicCenter.Instance.Dispose();
            }

            SystemCenter.Instance.Dispose();
            _state = RuntimeState.Idle;

            PlayerPrefs.Save();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        internal void EnterErrorState()
        {
            _state = RuntimeState.Error;
            LLog.Error("Enter <ErrorState>, please check log.");
            FrameworkError(FrameworkErrorCode.Startup, "System Startup error");
        }

        public void Tick(float deltaTime)
        {
            if (_restartWhenNextFrame)
            {
                RestartRuntime();
                return;
            }

            if (IsPause || _state == RuntimeState.Error)
            {
                return;
            }

#if UNITY_EDITOR
            var time = deltaTime * _setting.Common.TimeScale;
#else
            var time = deltaTime;
#endif

            SystemCenter.Instance.Tick(deltaTime);

            if (_state == RuntimeState.Running)
            {
                LogicCenter.Instance.Tick(time);
            }
        }

        public void Restart()
        {
            _restartWhenNextFrame = true;
        }

        private void RestartRuntime()
        {
            _restartWhenNextFrame = false;
            Debug.ClearDeveloperConsole();
            Shutdown();
            Startup(Launcher);
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
        public static DataSystem Data => Get<DataSystem>();
    }
}