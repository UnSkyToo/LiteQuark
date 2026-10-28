using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LoadAssetTask<T> : BaseTask, IResourceTaskCleanup where T : UnityEngine.Object
    {
        public override string DebugName => $"LoadAsset {_assetName}";

        private readonly AssetBundle _bundle;
        private readonly string _assetName;
        private Action<ITask> _callback;
        private AssetBundleRequest _assetRequest;
        private readonly UniTaskCompletionSource _cleanupCompletion = new();

        public UniTask WaitForCleanupAsync() => _cleanupCompletion.Task;

        public LoadAssetTask(AssetBundle bundle, string assetName, Action<ITask> callback)
            : base()
        {
            _bundle = bundle;
            _assetName = assetName;
            _callback = callback;
            _assetRequest = null;
        }

        protected override void OnDispose()
        {
            _callback = null;
        }

        public T GetAsset()
        {
            return Result as T;
        }

        protected override void OnTerminated()
        {
            if (State != TaskState.Succeeded && (_assetRequest == null || _assetRequest.isDone))
            {
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback, this);
            }

            if (_assetRequest == null && State != TaskState.Succeeded)
            {
                _cleanupCompletion.TrySetResult();
            }
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _assetRequest?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            _assetRequest = _bundle.LoadAssetAsync<T>(_assetName);

            if (_assetRequest == null)
            {
                Fail(new InvalidOperationException($"Could not start asset load: {_assetName}"));
                return;
            }

            _assetRequest.completed += OnAssetRequestLoadCompleted;
        }

        private void OnAssetRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnAssetRequestLoadCompleted;

            try
            {
                if (IsDone)
                {
                    return;
                }

                var asset = _assetRequest.asset as T;
                if (asset == null)
                {
                    Fail(new InvalidOperationException($"Asset load returned no {typeof(T).Name}: {_assetName}"));
                    return;
                }

                var callback = _callback;
                _callback = null;
                if (Complete(asset))
                {
                    LiteUtils.SafeInvoke(callback, this);
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
            finally
            {
                _assetRequest = null;
                _cleanupCompletion.TrySetResult();
            }
        }
    }
}
