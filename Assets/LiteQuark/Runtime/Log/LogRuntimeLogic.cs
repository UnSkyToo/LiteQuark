using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public class LogRuntimeLogic : ILogic
    {
        private GameObject Go_;
        
        public Task<bool> Initialize()
        {
            var setting = LiteRuntime.Setting.Log;
            var logEnable = setting.ReceiveLog && LiteRuntime.IsDebugMode;
            
            if (logEnable && setting.ShowLogViewer)
            {
                Go_ = Object.Instantiate(Resources.Load<GameObject>("IngameDebugConsole"));
                var scaler = Go_.GetOrAddComponent<CanvasScaler>();
                scaler.referencePixelsPerUnit = 100;
                scaler.uiScaleMode = LiteRuntime.Setting.UI.ScaleMode;
                scaler.screenMatchMode = LiteRuntime.Setting.UI.MatchMode;
                scaler.referenceResolution = new Vector2(LiteRuntime.Setting.UI.ResolutionWidth, LiteRuntime.Setting.UI.ResolutionHeight);
                scaler.matchWidthOrHeight = LiteRuntime.Setting.UI.MatchValue;
            }

            return Task.FromResult(true);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(Go_);
        }
        
        public void Tick(float deltaTime)
        {
        }
    }
}