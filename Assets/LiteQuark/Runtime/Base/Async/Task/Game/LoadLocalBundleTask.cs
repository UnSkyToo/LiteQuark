using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class LoadLocalBundleTask : BaseTask
    {
        private readonly string BundlePath_;
        private Action<AssetBundle> Callback_;
        private AssetBundleCreateRequest BundleRequest_;
        
        public LoadLocalBundleTask(string bundlePath, Action<AssetBundle> callback)
            : base()
        {
            BundlePath_ = bundlePath;
            Callback_ = callback;
        }

        public override void Dispose()
        {
            Callback_ = null;
        }

        protected override void OnExecute()
        {
            var fullPath = PathUtils.GetFullPathInRuntime(BundlePath_);
            BundleRequest_ = AssetBundle.LoadFromFileAsync(fullPath);
            BundleRequest_.completed += OnBundleRequestCompleted;
        }

        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            var bundle = (op as AssetBundleCreateRequest)?.assetBundle;
            Callback_?.Invoke(bundle != null ? bundle : null);
            Stop();
        }
    }
}