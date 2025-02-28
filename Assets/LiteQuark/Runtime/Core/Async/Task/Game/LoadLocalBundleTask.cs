using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LoadLocalBundleTask : BaseTask
    {
        private readonly string BundleUri_;
        private Action<AssetBundle> Callback_;
        private AssetBundleCreateRequest BundleRequest_;
        
        public LoadLocalBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            BundleUri_ = bundleUri;
            Callback_ = callback;
        }

        public override void Dispose()
        {
            Callback_ = null;
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
            Stop();
        }
    }
}