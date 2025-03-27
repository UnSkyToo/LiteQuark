using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadLocalBundleTask : LoadBundleBaseTask
    {
        private AssetBundleCreateRequest BundleRequest_;
        
        public LoadLocalBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base(bundleUri, callback)
        {
            Callback_ = callback;
        }
        
        public override AssetBundle WaitCompleted()
        {
            var bundle = BundleRequest_.assetBundle;
            return bundle;
        }

        protected override void OnExecute()
        {
            BundleRequest_ = AssetBundle.LoadFromFileAsync(BundleUri_);
            BundleRequest_.completed += OnBundleRequestCompleted;
        }
        
        protected override void OnTick(float deltaTime)
        {
            Progress = BundleRequest_?.progress ?? 0f;
        }

        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            var bundle = (op as AssetBundleCreateRequest)?.assetBundle;
            Callback_?.Invoke(bundle);
            Complete();
        }
    }
}