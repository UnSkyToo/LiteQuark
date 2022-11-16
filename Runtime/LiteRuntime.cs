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
        public AssetLoaderMode AssetMode
        {
            get
            {
#if UNITY_EDITOR
                return Launcher.AssetMode;
#else
                return AssetLoaderMode.Bundle;
#endif
            }
        }

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
            TimeScale = 1.0f;
            EnterBackgroundTime_ = 0.0f;
            RestartWhenNextFrame_ = false;
            LogicList_ = new List<ILogic>();
            Launcher = launcher;

            if (!LogManager.Instance.Startup())
            {
                return false;
            }

            LLog.Info("Lite Runtime Startup");

            if (!EventManager.Instance.Startup())
            {
                return false;
            }
            
            if (!TaskManager.Instance.Startup())
            {
                return false;
            }

            if (!AssetManager.Instance.Startup())
            {
                return false;
            }
            
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

            InitConfigure();
            IsPause = false;
            return true;
        }

        public void Shutdown()
        {
            OnEnterBackground();

            foreach (var logic in LogicList_)
            {
                logic.Shutdown();
            }
            LogicList_.Clear();

            AssetManager.Instance.Shutdown();
            TaskManager.Instance.Shutdown();
            EventManager.Instance.Shutdown();

            PlayerPrefs.Save();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            LLog.Info("Lite Runtime Shutdown");
            
            LogManager.Instance.Shutdown();
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
            TaskManager.Instance.Tick(time);

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
        
        private void InitConfigure()
        {
            Application.targetFrameRate = Launcher.TargetFrameRate;
            Input.multiTouchEnabled = Launcher.MultiTouch;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int) System.DateTime.Now.Ticks);
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

        public T Attach<T>() where T : MonoBehaviour
        {
            var Component = Launcher?.gameObject.GetComponent<T>();

            if (Component != null)
            {
                return Component;
            }

            return Launcher?.gameObject.AddComponent<T>();
        }

        public void Detach<T>() where T : MonoBehaviour
        {
            var Component = Launcher?.gameObject.GetComponent<T>();

            if (Component != null)
            {
                Object.DestroyImmediate(Component);
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
            EventManager.Instance.Send<EnterForegroundEvent>();

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
            EventManager.Instance.Send<EnterBackgroundEvent>();
            EnterBackgroundTime_ = Time.realtimeSinceStartup;
        }
    }
}