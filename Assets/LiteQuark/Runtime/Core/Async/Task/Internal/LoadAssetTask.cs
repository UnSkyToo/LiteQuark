using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class LoadAssetTask<T> : BaseTask where T : UnityEngine.Object
    {
        public override string DebugName => $"LoadAsset {_assetName}";

        private readonly AssetBundle _bundle;
        private readonly string _assetName;
        private Action<T> _callback;
        private AssetBundleRequest _assetRequest;

        public LoadAssetTask(AssetBundle bundle, string assetName, Action<T> callback)
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
            _assetRequest = null;
        }

        public T GetAsset()
        {
            return Result as T;
        }

        protected override void OnTerminated()
        {
            if (State != TaskState.Succeeded && (_assetRequest == null || _assetRequest.isDone))
            {
                NotifyCompleted(null);
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

            if (IsDone)
            {
                return;
            }

            T asset;
            try
            {
                asset = _assetRequest.asset as T;
                if (asset == null)
                {
                    Fail(new InvalidOperationException($"Asset load returned no {typeof(T).Name}: {_assetName}"));
                    return;
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }

            if (Complete(asset))
            {
                NotifyCompleted(asset);
            }
        }

        private void NotifyCompleted(T asset)
        {
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, asset);
        }
    }
}
