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
        private T _asset;

        public LoadAssetTask(AssetBundle bundle, string assetName, Action<T> callback)
            : base()
        {
            _bundle = bundle;
            _assetName = assetName;
            _callback = callback;
            _assetRequest = null;
            _asset = null;
        }

        public override void Dispose()
        {
            _callback = null;
            _assetRequest = null;
            _asset = null;
        }

        public T GetAsset()
        {
            return _asset;
        }

        public override void Cancel()
        {
            if (IsDone)
            {
                return;
            }

            base.Cancel();

            if (_assetRequest == null)
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
                Cancel();
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

            _asset = _assetRequest.asset as T;
            Complete(_asset);
            NotifyCompleted(_asset);
        }

        private void NotifyCompleted(T asset)
        {
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, asset);
        }
    }
}
