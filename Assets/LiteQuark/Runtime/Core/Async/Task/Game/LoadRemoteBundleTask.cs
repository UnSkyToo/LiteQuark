using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class LoadRemoteBundleTask : LoadBundleBaseTask
    {
        private UnityWebRequest Request_;
        
        public LoadRemoteBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base(bundleUri, callback)
        {
        }

        public override AssetBundle WaitCompleted()
        {
            throw new Exception($"{nameof(LoadRemoteBundleTask)} can't wait completed.");
        }

        protected override void OnExecute()
        {
            Request_ = UnityWebRequestAssetBundle.GetAssetBundle(new Uri(BundleUri_));
            var op = Request_.SendWebRequest();
            op.completed += OnBundleRequestCompleted;
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = Request_?.downloadProgress ?? 0f;
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