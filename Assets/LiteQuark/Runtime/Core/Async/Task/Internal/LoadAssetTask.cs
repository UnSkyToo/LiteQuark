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
        
        protected override void OnTick(float deltaTime)
        {
            Progress = _assetRequest?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            _assetRequest = _bundle.LoadAssetAsync<T>(_assetName);
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
            LiteUtils.SafeInvoke(_callback, _asset);
        }
    }
}