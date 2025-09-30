using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadAssetTask<T> : BaseTask where T : UnityEngine.Object
    {
        private readonly AssetBundle _bundle;
        private readonly string _assetName;
        private Action<UnityEngine.Object> _callback;
        private AssetBundleRequest _assetRequest;
        private UnityEngine.Object _asset;

        public LoadAssetTask(AssetBundle bundle, string assetName, Action<UnityEngine.Object> callback)
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
        
        public UnityEngine.Object GetAsset()
        {
            return _asset;
        }

        protected override void OnExecute()
        {
            _assetRequest = _bundle.LoadAssetAsync<T>(_assetName);
            _assetRequest.completed += OnAssetRequestLoadCompleted;
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _assetRequest?.progress ?? 0f;
        }

        private void OnAssetRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnAssetRequestLoadCompleted;
            _asset = _assetRequest.asset;
            
            LiteUtils.SafeInvoke(_callback, _asset);
            Complete(_asset);
        }
    }
}