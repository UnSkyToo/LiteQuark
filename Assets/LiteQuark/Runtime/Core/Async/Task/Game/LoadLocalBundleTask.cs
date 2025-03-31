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
        }
        
        public override AssetBundle WaitCompleted()
        {
            foreach (var childTask in ChildTasks_)
            {
                childTask.WaitCompleted();
            }
            
            var bundle = BundleRequest_.assetBundle;
            return bundle;
        }

        protected override float GetDownloadPercent()
        {
            return BundleRequest_?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            BundleRequest_ = AssetBundle.LoadFromFileAsync(BundleUri_);
            BundleRequest_.completed += OnBundleRequestCompleted;
        }
        
        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            OnBundleLoaded(BundleRequest_.assetBundle);
        }
    }
}