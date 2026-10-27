using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public class LogRuntimeLogic : ILogic
    {
        private GameObject _go;

        public UniTask<bool> Initialize()
        {
            var setting = LiteRuntime.Setting.Log;
            var logEnable = setting.ReceiveLog && LiteRuntime.IsDebugMode;

            if (logEnable && setting.ShowLogViewer)
            {
                var canvasScaler = Object.FindFirstObjectByType<CanvasScaler>();
                _go = Object.Instantiate(Resources.Load<GameObject>("IngameDebugConsole"));
                if (canvasScaler != null)
                {
                    var scaler = _go.GetOrAddComponent<CanvasScaler>();
                    scaler.uiScaleMode = canvasScaler.uiScaleMode;
                    scaler.screenMatchMode = canvasScaler.screenMatchMode;
                    scaler.referenceResolution = canvasScaler.referenceResolution;
                    scaler.referencePixelsPerUnit = canvasScaler.referencePixelsPerUnit;
                    scaler.matchWidthOrHeight = canvasScaler.matchWidthOrHeight;
                }
            }

            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            Object.Destroy(_go);

            return  UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
        }
    }
}
