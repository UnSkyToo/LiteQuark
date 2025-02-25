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
            var logEnable = setting.ReceiveLog && LiteRuntime.IsDebugMode;
            
            if (logEnable && setting.ShowLogViewer)
            {
                Go_ = Object.Instantiate(Resources.Load<GameObject>("IngameDebugConsole"));
                var scaler = Go_.GetOrAddComponent<CanvasScaler>();
                scaler.referencePixelsPerUnit = 100;
#if LITE_QUARK_ENABLE_UI
                scaler.uiScaleMode = LiteRuntime.Setting.UI.ScaleMode;
                scaler.screenMatchMode = LiteRuntime.Setting.UI.MatchMode;
                scaler.referenceResolution = new Vector2(LiteRuntime.Setting.UI.ResolutionWidth, LiteRuntime.Setting.UI.ResolutionHeight);
                scaler.matchWidthOrHeight = LiteRuntime.Setting.UI.MatchValue;
#else
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0;
#endif
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