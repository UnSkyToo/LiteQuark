#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSimulateAssetTask<T> : BaseTask, IResourceTaskCleanup where T : UnityEngine.Object
    {
        public override string DebugName => $"SimulateAsset {_assetPath}";

        private readonly string _assetPath;
        private Action<ITask> _callback;
        private T _asset;
        private readonly UniTaskCompletionSource _cleanupCompletion = new();

        public LoadSimulateAssetTask(string assetPath, Action<ITask> callback)
        {
            _assetPath = assetPath;
            _callback = callback;
        }

        public UniTask WaitForCleanupAsync() => _cleanupCompletion.Task;

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
            try
            {
                if (Complete(asset))
                {
                    LiteUtils.SafeInvoke(callback, this);
                }
            }
            finally
            {
                _cleanupCompletion.TrySetResult();
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
            _cleanupCompletion.TrySetResult();
        }

        protected override void OnDispose()
        {
            _callback = null;
        }
    }
}
#endif
