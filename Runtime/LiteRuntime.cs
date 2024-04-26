using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LiteRuntime : Singleton<LiteRuntime>
    {
        public bool IsPause { get; set; }
        public bool IsFocus { get; private set; }

        public float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }

        public LiteLauncher Launcher { get; private set; }

        private List<ISystem> SystemList_ = new List<ISystem>();
        private Dictionary<System.Type, ISystem> SystemTypeMap_ = new Dictionary<System.Type, ISystem>();

        private List<ILogic> LogicList_ = new List<ILogic>();
        
        private float EnterBackgroundTime_ = 0.0f;
        private bool RestartWhenNextFrame_ = false;

        public LiteRuntime()
        {
        }
        
        public bool Startup(LiteLauncher launcher)
        {
            IsPause = true;
            IsFocus = true;
            Launcher = launcher;

            if (!InitializeSystem())
            {
                return false;
            }

            if (!InitializeLogic())
            {
                return false;
            }

            InitializeConfigure();
            
            IsPause = false;
            
            return true;
        }

        public void Shutdown()
        {
            OnEnterBackground();

            UnInitializeLogic();
            UnInitializeSystem();

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

            var time = deltaTime * TimeScale;

            foreach (var system in SystemList_)
            {
                if (system is ITick tickSys)
                {
                    tickSys.Tick(time);
                }
            }

            foreach (var logic in LogicList_)
            {
                logic.Tick(time);
            }
        }

        public void OnGUI()
        {
            foreach (var logic in LogicList_)
            {
                if (logic is IOnGUI guiLogic)
                {
                    guiLogic.OnGUI();
                }
            }
        }

        private bool InitializeSystem()
        {
            try
            {
                SystemList_.Clear();
                SystemTypeMap_.Clear();

                foreach (var type in LiteConst.SystemTypeList)
                {
                    LLog.Info($"Initialize {type}");
                    if (System.Activator.CreateInstance(type) is ISystem sys)
                    {
                        SystemList_.Add(sys);
                        SystemTypeMap_.Add(type, sys);
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                LLog.Exception(ex);
                return false;
            }
        }

        private void UnInitializeSystem()
        {
            for (var index = SystemList_.Count - 1; index >= 0; --index)
            {
                SystemList_[index].Dispose();
            }
            
            SystemList_.Clear();
            SystemTypeMap_.Clear();
        }

        private bool InitializeLogic()
        {
            try
            {
                LogicList_.Clear();
                
                foreach (var logicEntry in Launcher.Setting.LogicList)
                {
                    if (logicEntry.Disabled)
                    {
                        continue;
                    }
                    
                    LLog.Info($"initialize {logicEntry.TypeName} system");

                    var logicType = TypeUtils.GetTypeWithAssembly(logicEntry.AssemblyName, logicEntry.TypeName);
                    if (logicType == null)
                    {
                        LLog.Error($"can't not find logic class type : {logicEntry.TypeName}");
                        continue;
                    }

                    if (System.Activator.CreateInstance(logicType) is not ILogic logic)
                    {
                        LLog.Error($"incorrect logic class type : {logicEntry.TypeName}");
                        continue;
                    }

                    if (!logic.Startup())
                    {
                        LLog.Error($"{logicEntry.TypeName} startup failed");
                        continue;
                    }

                    LogicList_.Add(logic);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                LLog.Exception(ex);
                return false;
            }
        }

        private void UnInitializeLogic()
        {
            foreach (var logic in LogicList_)
            {
                logic.Shutdown();
            }
            LogicList_.Clear();
        }
        
        private void InitializeConfigure()
        {
            Application.targetFrameRate = Launcher.Setting.Common.TargetFrameRate;
            Input.multiTouchEnabled = Launcher.Setting.Common.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int) System.DateTime.Now.Ticks);
            
            TimeScale = 1.0f;
            EnterBackgroundTime_ = 0.0f;
            RestartWhenNextFrame_ = false;
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
            IsPause = !Startup(Launcher);
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
            GetSystem<EventSystem>().Send<EnterForegroundEvent>();

            if (Launcher.Setting.Common.AutoRestartInBackground && Time.realtimeSinceStartup - EnterBackgroundTime_ >= Launcher.Setting.Common.BackgroundLimitTime)
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
            GetSystem<EventSystem>().Send<EnterBackgroundEvent>();
            EnterBackgroundTime_ = Time.realtimeSinceStartup;
        }

        public T GetSystem<T>() where T : ISystem
        {
            if (SystemTypeMap_.TryGetValue(typeof(T), out var system))
            {
                return (T) system;
            }

            return default;
        }

        /// <summary>
        /// Get System
        /// </summary>
        public static T Get<T>() where T : ISystem
        {
            return Instance.GetSystem<T>();
        }

        public static LiteSetting Setting => Instance.Launcher.Setting;
        
        // frequently used system
        public static LogSystem Log => Get<LogSystem>();
        public static ObjectPoolSystem ObjectPool => Get<ObjectPoolSystem>();
        public static EventSystem Event => Get<EventSystem>();
        public static TaskSystem Task => Get<TaskSystem>();
        public static TimerSystem Timer => Get<TimerSystem>();
        public static GroupSystem Group => Get<GroupSystem>();
        public static AssetSystem Asset => Get<AssetSystem>();
        public static AudioSystem Audio => Get<AudioSystem>();
        public static ConfigSystem Config => Get<ConfigSystem>();
        public static UISystem UI => Get<UISystem>();
    }
}