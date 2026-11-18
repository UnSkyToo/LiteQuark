#if UNITY_EDITOR
using System;
using UnityEditor;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSimulateAssetTask<T> : BaseTask where T : UnityEngine.Object
    {
        public override string DebugName => $"SimulateAsset {_assetPath}";

        private readonly string _assetPath;
        private Action<ITask> _callback;
        private T _asset;

        public LoadSimulateAssetTask(string assetPath, Action<ITask> callback)
        {
            _assetPath = assetPath;
            _callback = callback;
        }

        protected override void OnExecute()
        {
            _asset = AssetDatabase.LoadAssetAtPath<T>(_assetPath);
            if (_asset == null)
            {
                Fail(new InvalidOperationException($"Editor asset load returned no {typeof(T).Name}: {_assetPath}"));
            }
        }

        protected override void OnTick(float deltaTime)
        {
            var asset = _asset;
            _asset = null;
            var callback = _callback;
            _callback = null;
            if (Complete(asset))
            {
                LiteUtils.SafeInvoke(callback, this);
            }
        }

        protected override void OnTerminated()
        {
            if (State == TaskState.Succeeded)
            {
                return;
            }
            _asset = null;
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, this);
        }

        protected override void OnDispose()
        {
            _callback = null;
        }
    }
}
#endif
