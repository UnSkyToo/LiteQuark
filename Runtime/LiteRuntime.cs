using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LiteRuntime : Singleton<LiteRuntime>
    {
        public bool IsPause { get; set; }
        public bool IsRestart { get; private set; }
        public bool IsFocus { get; private set; }

        public float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }

        public MonoBehaviour MonoBehaviourInstance { get; private set; }
        
        private float EnterBackgroundTime_ = 0.0f;
        private IGameLogic MainLogic_;

        public LiteRuntime()
        {
        }
        
        public bool Startup(MonoBehaviour instance, IGameLogic logic)
        {
            IsPause = true;
            IsRestart = false;
            IsFocus = true;
            TimeScale = 1.0f;
            MonoBehaviourInstance = instance;

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

            MainLogic_ = logic;
            if (MainLogic_ == null || !MainLogic_.Startup())
            {
                LLog.Error("Logic Startup Failed");
                return false;
            }

            InitConfigure();
            IsPause = false;
            return true;
        }

        public void Shutdown()
        {
            OnEnterBackground();

            MainLogic_?.Shutdown();

            AssetManager.Instance.Shutdown();
            TaskManager.Instance.Shutdown();
            EventManager.Instance.Shutdown();

            PlayerPrefs.Save();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            LLog.Info("Lite Runtime Shutdown");
        }

        public void Tick(float deltaTime)
        {
            if (IsRestart)
            {
                RestartGameManager();
                return;
            }

            if (IsPause)
            {
                return;
            }

            var time = deltaTime * TimeScale;
            TaskManager.Instance.Tick(time);
            MainLogic_?.Tick(time);
        }

        private void InitConfigure()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Random.InitState((int) System.DateTime.Now.Ticks);
        }

        public void Restart()
        {
            IsRestart = true;
        }

        private void RestartGameManager()
        {
            IsRestart = false;
            Debug.ClearDeveloperConsole();
            Shutdown();
            IsPause = !Startup(MonoBehaviourInstance, MainLogic_);
        }

        public T Attach<T>() where T : MonoBehaviour
        {
            var Component = MonoBehaviourInstance?.gameObject.GetComponent<T>();

            if (Component != null)
            {
                return Component;
            }

            return MonoBehaviourInstance?.gameObject.AddComponent<T>();
        }

        public void Detach<T>() where T : MonoBehaviour
        {
            var Component = MonoBehaviourInstance?.gameObject.GetComponent<T>();

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

            LLog.Warning("OnEnterForeground");
            EventManager.Instance.Send<EnterForegroundEvent>();

            if (LiteConst.EnterBackgroundAutoRestart && Time.realtimeSinceStartup - EnterBackgroundTime_ >= LiteConst.EnterBackgroundMaxTime)
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
 
            LLog.Warning("OnEnterBackground");
            EventManager.Instance.Send<EnterBackgroundEvent>();
            EnterBackgroundTime_ = Time.realtimeSinceStartup;
        }
    }
}