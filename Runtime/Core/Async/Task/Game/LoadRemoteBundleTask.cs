using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class LoadRemoteBundleTask : LoadBundleBaseTask
    {
        private readonly Uri BundleUri_;
        private UnityWebRequest Request_;
        private UnityWebRequestAsyncOperation AsyncOperation_;
        
        public LoadRemoteBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base(callback)
        {
            BundleUri_ = new Uri(bundleUri);
            Callback_ = callback;
        }

        public override AssetBundle WaitCompleted()
        {
            return null;
        }

        protected override void OnExecute()
        {
            Request_ = UnityWebRequestAssetBundle.GetAssetBundle(BundleUri_);
            var op = Request_.SendWebRequest();
            op.completed += OnBundleRequestCompleted;
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