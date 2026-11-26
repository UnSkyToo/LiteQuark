using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    internal sealed class LogSystem : ISystem
    {
        private GameObject _viewer;

        public UniTask<bool> Initialize(CancellationToken ct = default) => UniTask.FromResult(true);

        internal void OpenViewer()
        {
            var setting = LiteRuntime.Setting.Log;
            if (!setting.ShowLogViewer)
            {
                return;
            }

            var canvasScaler = Object.FindFirstObjectByType<CanvasScaler>();
            _viewer = Object.Instantiate(Resources.Load<GameObject>("IngameDebugConsole"));
            if (canvasScaler != null)
            {
                var scaler = _viewer.GetOrAddComponent<CanvasScaler>();
                scaler.uiScaleMode = canvasScaler.uiScaleMode;
                scaler.screenMatchMode = canvasScaler.screenMatchMode;
                scaler.referenceResolution = canvasScaler.referenceResolution;
                scaler.referencePixelsPerUnit = canvasScaler.referencePixelsPerUnit;
                scaler.matchWidthOrHeight = canvasScaler.matchWidthOrHeight;
            }
        }

        public UniTask DisposeAsync()
        {
            if (_viewer != null)
            {
                Object.Destroy(_viewer);
            }

            _viewer = null;
            return UniTask.CompletedTask;
        }
    }
}
