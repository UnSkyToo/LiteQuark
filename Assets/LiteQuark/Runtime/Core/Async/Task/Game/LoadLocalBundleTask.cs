using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadLocalBundleTask : LoadBundleBaseTask
    {
        private readonly string BundleUri_;
        private AssetBundleCreateRequest BundleRequest_;
        
        public LoadLocalBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base(callback)
        {
            BundleUri_ = bundleUri;
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

        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            var bundle = (op as AssetBundleCreateRequest)?.assetBundle;
            Callback_?.Invoke(bundle);
            Complete();
        }
    }
}