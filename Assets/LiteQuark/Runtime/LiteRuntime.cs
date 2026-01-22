using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed partial class LiteRuntime : Singleton<LiteRuntime>
    {
        public static event System.Action<FrameworkErrorCode, string> OnFrameworkError;
        
        public static bool IsDebugMode { get; private set; } = false;
        
        public bool IsPause { get; set; }
        public bool IsFocus { get; private set; }

        public LiteLauncher Launcher { get; private set; }
        
        private LiteSetting _setting = null;
        private float _enterBackgroundTime = 0.0f;
        private bool _restartWhenNextFrame = false;

        private StageCenter _stageCenter;

        private LiteRuntime()
        {
        }
        
        public void Startup(LiteLauncher launcher)
        {
            IsPause = false;
            IsFocus = true;
            Launcher = launcher;
            _setting = launcher.Setting;
            IsDebugMode = Debug.isDebugBuild && _setting.Debug.DebugMode;
            _enterBackgroundTime = 0.0f;
            _restartWhenNextFrame = false;

            _stageCenter = new StageCenter();
        }

        public void Shutdown()
        {
            OnEnterBackground();
            
            if (_stageCenter != null)
            {
                _stageCenter.Dispose();
                _stageCenter = null;
            }
            
            SystemCenter.Instance.Dispose();

            PlayerPrefs.Save();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        internal void ErrorStage()
        {
            _stageCenter?.ErrorStage();
        }

        public void Tick(float deltaTime)
        {
            if (_restartWhenNextFrame)
            {
                RestartRuntime();
                return;
            }

            if (IsPause)
            {
                return;
            }

#if UNITY_EDITOR
            var time = deltaTime * _setting.Debug.TimeScale;
#else
            var time = deltaTime;
#endif
            
            SystemCenter.Instance.Tick(deltaTime);
            _stageCenter.Tick(time);
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