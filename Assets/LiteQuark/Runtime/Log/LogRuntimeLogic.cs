using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LogRuntimeLogic : ILogic
    {
        private GameObject Go_;
        
        public bool Startup()
        {
            var launcher = LiteRuntime.Instance.Launcher;
            if (launcher.ReceiveLog)
            {
                LiteRuntime.GetLogSystem().GetRepository().EnableLevel(LogLevel.Info, launcher.LogInfo);
                LiteRuntime.GetLogSystem().GetRepository().EnableLevel(LogLevel.Warn, launcher.LogWarn);
                LiteRuntime.GetLogSystem().GetRepository().EnableLevel(LogLevel.Error, launcher.LogError);
                LiteRuntime.GetLogSystem().GetRepository().EnableLevel(LogLevel.Fatal, launcher.LogFatal);
            }
            else
            {
                LiteRuntime.GetLogSystem().GetRepository().EnableLevel(LogLevel.All, false);
            }
            
            if (launcher.ReceiveLog && launcher.ShowLogViewer)
            {
                Go_ = Object.Instantiate(Resources.Load<GameObject>("IngameDebugConsole"));
            }
            
            return true;
        }

        public void Shutdown()
        {
            Object.DestroyImmediate(Go_);
        }
        
        public void Tick(float deltaTime)
        {
        }
    }
}