using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LogRuntimeLogic : ILogic
    {
        public bool Startup()
        {
            var launcher = LiteRuntime.Instance.Launcher;
            if (launcher.ReceiveLog)
            {
                LogManager.Instance.GetRepository().EnableLevel(LogLevel.Info, launcher.LogInfo);
                LogManager.Instance.GetRepository().EnableLevel(LogLevel.Warn, launcher.LogWarn);
                LogManager.Instance.GetRepository().EnableLevel(LogLevel.Error, launcher.LogError);
                LogManager.Instance.GetRepository().EnableLevel(LogLevel.Fatal, launcher.LogFatal);
            }
            else
            {
                LogManager.Instance.GetRepository().EnableLevel(LogLevel.All, false);
            }

            var go = GameObject.Find("IngameDebugConsole");
            if (go != null)
            {
                go.SetActive(launcher.ReceiveLog && launcher.ShowLogViewer);
            }
            
            return true;
        }

        public void Shutdown()
        {
        }
        
        public void Tick(float deltaTime)
        {
        }
    }
}