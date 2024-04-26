using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LogRuntimeLogic : ILogic
    {
        private GameObject Go_;
        
        public bool Startup()
        {
            var setting = LiteRuntime.Setting.Log;
            if (setting.ReceiveLog)
            {
                LiteRuntime.Log.GetRepository().EnableLevel(LogLevel.Info, setting.LogInfo);
                LiteRuntime.Log.GetRepository().EnableLevel(LogLevel.Warn, setting.LogWarn);
                LiteRuntime.Log.GetRepository().EnableLevel(LogLevel.Error, setting.LogError);
                LiteRuntime.Log.GetRepository().EnableLevel(LogLevel.Fatal, setting.LogFatal);
            }
            else
            {
                LiteRuntime.Log.GetRepository().EnableLevel(LogLevel.All, false);
            }
            
            if (setting.ReceiveLog && setting.ShowLogViewer)
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