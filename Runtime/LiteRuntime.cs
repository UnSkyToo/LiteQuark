using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed partial class LiteRuntime : Singleton<LiteRuntime>
    {
        public static bool IsDebugMode { get; private set; } = false;
        
        public bool IsPause { get; set; }
        public bool IsFocus { get; private set; }

        public LiteLauncher Launcher { get; private set; }
        
        private static readonly Dictionary<System.Type, int> RegisterSystemMap_ = new Dictionary<System.Type, int>();
        private readonly List<ISystem> SystemList_ = new List<ISystem>();
        private readonly Dictionary<System.Type, ISystem> SystemTypeMap_ = new Dictionary<System.Type, ISystem>();
        
        private readonly List<ILogic> LogicList_ = new List<ILogic>();
        private readonly List<ILogic> LogicAddList_ = new List<ILogic>();
        
        private LiteSetting Setting_ = null;
        private float EnterBackgroundTime_ = 0.0f;
        private bool RestartWhenNextFrame_ = false;

        public LiteRuntime()
        {
        }
        
        public void Startup(LiteLauncher launcher)
        {
            IsPause = false;
            IsFocus = true;
            Launcher = launcher;
            Setting_ = launcher.Setting;
            IsDebugMode = Debug.isDebugBuild && Setting_.Debug.DebugMode;
            
            InitializeConfigure();
            
            InitializeSystem();
            
            InitializeLogic();
        }

        public void Shutdown()
        {
            OnEnterBackground();

            ProcessLogicAdd();
            
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
            
            ProcessLogicAdd();

#if UNITY_EDITOR
            var time = deltaTime * Setting_.Debug.TimeScale;
#else
            var time = deltaTime;
#endif

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

        private void InitializeSystem()
        {
            SystemList_.Clear();
            SystemTypeMap_.Clear();

            var registerSystemList = new List<System.Type>(RegisterSystemMap_.Keys);
            registerSystemList.Sort((x, y) => RegisterSystemMap_[y].CompareTo(RegisterSystemMap_[x]));
            
            foreach (var type in registerSystemList)
            {
                LLog.Info($"Initialize {type.AssemblyQualifiedName}");
                if (System.Activator.CreateInstance(type) is ISystem sys)
                {
                    SystemList_.Add(sys);
                    SystemTypeMap_.Add(type, sys);
                }
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

        private void InitializeLogic()
        {
            LogicList_.Clear();
            
            foreach (var logicEntry in Setting_.LogicList)
            {
                if (logicEntry.Disabled)
                {
                    continue;
                }
                
                LLog.Info($"initialize {logicEntry.AssemblyQualifiedName} system");

                var logicType = System.Type.GetType(logicEntry.AssemblyQualifiedName);
                if (logicType == null)
                {
                    throw new System.Exception($"can't not find logic class type : {logicEntry.AssemblyQualifiedName}");
                }

                if (System.Activator.CreateInstance(logicType) is not ILogic logic)
                {
                    throw new System.Exception($"incorrect logic class type : {logicEntry.AssemblyQualifiedName}");
                }

                if (!logic.Startup())
                {
                    throw new System.Exception($"{logicEntry.AssemblyQualifiedName} startup failed");
                }

                LogicList_.Add(logic);
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

        public void AddLogic(ILogic logic)
        {
            LogicAddList_.Add(logic);
        }

        private void ProcessLogicAdd()
        {
            if (LogicAddList_.Count > 0)
            {
                foreach (var logic in LogicAddList_)
                {
                    if (logic == null)
                    {
                        continue;
                    }
                    
                    if (!logic.Startup())
                    {
                        throw new System.Exception($"{logic.GetType().Name} startup failed");
                    }
                    
                    LogicList_.Add(logic);
                }
                LogicAddList_.Clear();
            }
        }
        
        private void InitializeConfigure()
        {
            Application.targetFrameRate = Setting_.Common.TargetFrameRate;
            Input.multiTouchEnabled = Setting_.Common.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int) System.DateTime.Now.Ticks);
            
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
            GetSystem<EventSystem>().Send<EnterForegroundEvent>();

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
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterSystem()
        {
            RegisterSystemMap_.Clear();
            
            RegisterSystem<LogSystem>(99900);
            RegisterSystem<EventSystem>(99800);
            RegisterSystem<TaskSystem>(99700);
            RegisterSystem<TimerSystem>(99600);
            RegisterSystem<GroupSystem>(99500);
            RegisterSystem<AssetSystem>(99400);
            RegisterSystem<ObjectPoolSystem>(99300);
            RegisterSystem<AudioSystem>(99200);
            RegisterSystem<ActionSystem>(99100);
        }

        /// <summary>
        /// Register LiteQuark runtime module
        /// </summary>
        /// <param name="priority">Sort by priority value from high to low, can't greater than 90000</param>
        public static void RegisterSystem<T>(int priority) where T : ISystem
        {
            var type = typeof(T);
            if (RegisterSystemMap_.ContainsKey(type))
            {
                return;
            }
            
            RegisterSystemMap_.Add(type, priority);
        }
    }
}