using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class LoadRemoteBundleTask : BaseTask
    {
        private readonly Uri BundleUri_;
        private Action<AssetBundle> Callback_;
        private UnityWebRequest Request_;
        
        public LoadRemoteBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base()
        {
            BundleUri_ = new Uri(bundleUri);
            Callback_ = callback;
        }

        public override void Dispose()
        {
            Callback_ = null;
        }

        protected override void OnExecute()
        {
            Request_ = UnityWebRequestAssetBundle.GetAssetBundle(BundleUri_);
            var asyncOperation = Request_.SendWebRequest();
            asyncOperation.completed += OnBundleRequestCompleted;
        }
        
        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            if (Request_.result != UnityWebRequest.Result.Success)
            {
                LLog.Error($"Failed to download bundle : {BundleUri_}");
                Callback_?.Invoke(null);
                Abort();
            }
            else
            {
                var bundle = DownloadHandlerAssetBundle.GetContent(Request_);
                Callback_?.Invoke(bundle);
                Complete();
            }
        }
    }
}