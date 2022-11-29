using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LiteRuntime : Singleton<LiteRuntime>
    {
        public bool IsPause { get; set; }
        public bool IsFocus { get; private set; }

        public float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }

        public LiteLauncher Launcher { get; private set; }

        private LogSystem LogSystem_ = null;
        private ObjectPoolSystem ObjectPoolSystem_ = null;
        private EventSystem EventSystem_ = null;
        private TaskSystem TaskSystem_ = null;
        private TimerSystem TimerSystem_ = null;
        private AssetSystem AssetSystem_ = null;

        private float EnterBackgroundTime_ = 0.0f;
        private bool RestartWhenNextFrame_ = false;
        private List<ILogic> LogicList_;

        public LiteRuntime()
        {
        }
        
        public bool Startup(LiteLauncher launcher)
        {
            IsPause = true;
            IsFocus = true;
            Launcher = launcher;

            LogSystem_ = new LogSystem();

            LLog.Info("Lite Runtime Startup");

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

            LLog.Info("Lite Runtime Shutdown");
            
            LogSystem_.Dispose();
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
            TaskSystem_.Tick(time);
            TimerSystem_.Tick(time);

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
                ObjectPoolSystem_ = new ObjectPoolSystem();
                EventSystem_ = new EventSystem();
                TaskSystem_ = new TaskSystem();
                TimerSystem_ = new TimerSystem();

#if UNITY_EDITOR
                AssetSystem_ = new AssetSystem(Launcher.AssetMode);
#else
                AssetSystem_ = new AssetSystem(AssetLoaderMode.Bundle);
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void UnInitializeSystem()
        {
            AssetSystem_.Dispose();
            TimerSystem_.Dispose();
            TaskSystem_.Dispose();
            EventSystem_.Dispose();
            ObjectPoolSystem_.Dispose();
        }

        private bool InitializeLogic()
        {
            try
            {
                LogicList_ = new List<ILogic>();
                
                foreach (var logicEntry in Launcher.LogicList)
                {
                    if (logicEntry.Disabled)
                    {
                        continue;
                    }

                    var logicType = TypeHelper.GetTypeWithAssembly(logicEntry.AssemblyName, logicEntry.TypeName);
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
            catch
            {
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
            Application.targetFrameRate = Launcher.TargetFrameRate;
            Input.multiTouchEnabled = Launcher.MultiTouch;
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
            EventSystem_.Send<EnterForegroundEvent>();

            if (Launcher.AutoRestartInBackground && Time.realtimeSinceStartup - EnterBackgroundTime_ >= Launcher.BackgroundLimitTime)
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
            EventSystem_.Send<EnterBackgroundEvent>();
            EnterBackgroundTime_ = Time.realtimeSinceStartup;
        }

        public static LogSystem GetLogSystem()
        {
            return Instance.LogSystem_;
        }

        public static ObjectPoolSystem GetObjectPoolSystem()
        {
            return Instance.ObjectPoolSystem_;
        }

        public static EventSystem GetEventSystem()
        {
            return Instance.EventSystem_;
        }

        public static TaskSystem GetTaskSystem()
        {
            return Instance.TaskSystem_;
        }

        public static TimerSystem GetTimerSystem()
        {
            return Instance.TimerSystem_;
        }
        
        public static AssetSystem GetAssetSystem()
        {
            return Instance.AssetSystem_;
        }
    }
}