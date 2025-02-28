using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed partial class LiteRuntime : Singleton<LiteRuntime>
    {
        public static bool IsDebugMode { get; private set; } = false;
        
        public bool IsPause { get; set; }
        public bool IsFocus { get; private set; }

        public LiteLauncher Launcher { get; private set; }
        
        private LiteSetting Setting_ = null;
        private float EnterBackgroundTime_ = 0.0f;
        private bool RestartWhenNextFrame_ = false;

        private StageCenter StageCenter_;

        private LiteRuntime()
        {
        }
        
        public void Startup(LiteLauncher launcher)
        {
            IsPause = false;
            IsFocus = true;
            Launcher = launcher;
            Setting_ = launcher.Setting;
            IsDebugMode = Debug.isDebugBuild && Setting_.Debug.DebugMode;
            EnterBackgroundTime_ = 0.0f;
            RestartWhenNextFrame_ = false;

            StageCenter_ = new StageCenter();
        }

        public void Shutdown()
        {
            OnEnterBackground();

            if (StageCenter_ != null)
            {
                StageCenter_.Dispose();
                StageCenter_ = null;
            }

            PlayerPrefs.Save();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        public void Tick(float deltaTime)
        {
            if (RestartWhenNextFrame_)
            {
                RestartRuntime();
                return;
            }

            if (IsPause)
            {
                return;
            }

#if UNITY_EDITOR
            var time = deltaTime * Setting_.Debug.TimeScale;
#else
            var time = deltaTime;
#endif
            
            StageCenter_.Tick(time);
        }

        public void Restart()
        {
            RestartWhenNextFrame_ = true;
        }

        private void RestartRuntime()
        {
            RestartWhenNextFrame_ = false;
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
            Event.Send<EnterForegroundEvent>();

            if (Setting_.Common.AutoRestartInBackground && Time.realtimeSinceStartup - EnterBackgroundTime_ >= Setting_.Common.BackgroundLimitTime)
            {
                Restart();
                return;
            }

            EnterBackgroundTime_ = Time.realtimeSinceStartup;
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
            Event.Send<EnterBackgroundEvent>();
            EnterBackgroundTime_ = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Get System
        /// </summary>
        public static T Get<T>() where T : ISystem
        {
            return SystemCenter.Instance.GetSystem<T>();
        }

        public static LiteSetting Setting => Instance.Setting_;
        
        // frequently used system
        public static LogSystem Log => Get<LogSystem>();
        public static EventSystem Event => Get<EventSystem>();
        public static TaskSystem Task => Get<TaskSystem>();
        public static TimerSystem Timer => Get<TimerSystem>();
        public static GroupSystem Group => Get<GroupSystem>();
        public static AssetSystem Asset => Get<AssetSystem>();
        public static ObjectPoolSystem ObjectPool => Get<ObjectPoolSystem>();
        public static ActionSystem Action => Get<ActionSystem>();
        public static AudioSystem Audio => Get<AudioSystem>();
        
        /// <summary>
        /// Register LiteQuark runtime module
        /// </summary>
        /// <param name="priority">Sort by priority value from high to low, can't greater than 90000</param>
        public static void RegisterSystem<T>(int priority) where T : ISystem
        {
            SystemCenter.RegisterSystem<T>(priority);
        }
    }
}