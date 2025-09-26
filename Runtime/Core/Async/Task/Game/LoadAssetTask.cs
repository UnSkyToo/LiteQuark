using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadAssetTask<T> : LoadAssetBaseTask where T : UnityEngine.Object
    {
        private AssetBundleRequest _assetRequest;

        public LoadAssetTask(AssetBundle bundle, string name, Action<UnityEngine.Object> callback)
            : base(bundle, name, callback)
        {
        }

        protected override void OnExecute()
        {
            _assetRequest = Bundle.LoadAssetAsync<T>(Name);
            _assetRequest.completed += OnAssetRequestLoadCompleted;
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _assetRequest?.progress ?? 0f;
        }

        private void OnAssetRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnAssetRequestLoadCompleted;
            OnAssetLoaded(_assetRequest.asset);
        }
    }
}