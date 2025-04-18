using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadAssetTask<T> : LoadAssetBaseTask where T : UnityEngine.Object
    {
        private AssetBundleRequest AssetRequest_;

        public LoadAssetTask(AssetBundle bundle, string name, Action<UnityEngine.Object> callback)
            : base(bundle, name, callback)
        {
        }

        protected override void OnExecute()
        {
            AssetRequest_ = Bundle_.LoadAssetAsync<T>(Name_);
            AssetRequest_.completed += OnAssetRequestLoadCompleted;
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = AssetRequest_?.progress ?? 0f;
        }

        private void OnAssetRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnAssetRequestLoadCompleted;

            var asset = (op as AssetBundleRequest)?.asset;
            Callback_?.Invoke(asset);
            Complete(asset);
        }

        public override UnityEngine.Object WaitCompleted()
        {
            return AssetRequest_.asset;
        }
    }
}