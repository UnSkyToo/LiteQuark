using UnityEngine;
using UnityEngine.UI;

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
                var scaler = Go_.GetOrAddComponent<CanvasScaler>();
                scaler.uiScaleMode = LiteRuntime.Setting.UI.ScaleMode;
                scaler.screenMatchMode = LiteRuntime.Setting.UI.MatchMode;
                scaler.referenceResolution = new Vector2(LiteRuntime.Setting.UI.ResolutionWidth, LiteRuntime.Setting.UI.ResolutionHeight);
                scaler.matchWidthOrHeight = LiteRuntime.Setting.UI.MatchValue;
                scaler.referencePixelsPerUnit = 100;
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