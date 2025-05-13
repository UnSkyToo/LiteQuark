using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadLocalBundleTask : LoadBundleBaseTask
    {
        private AssetBundleCreateRequest _bundleRequest;
        
        public LoadLocalBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base(bundleUri, callback)
        {
        }
        
        public override AssetBundle WaitCompleted()
        {
            foreach (var childTask in ChildTasks)
            {
                childTask.WaitCompleted();
            }
            
            var bundle = _bundleRequest.assetBundle;
            return bundle;
        }

        protected override float GetDownloadPercent()
        {
            return _bundleRequest?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            _bundleRequest = AssetBundle.LoadFromFileAsync(BundleUri);
            _bundleRequest.completed += OnBundleRequestCompleted;
        }
        
        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            OnBundleLoaded(_bundleRequest.assetBundle);
        }
    }
}