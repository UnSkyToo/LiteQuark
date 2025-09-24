using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class LoadRemoteBundleTask : LoadBundleBaseTask
    {
        private UnityWebRequest _request;
        
        public LoadRemoteBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base(bundleUri, callback)
        {
        }

        public override void Dispose()
        {
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }
            
            base.Dispose();
        }

        public override AssetBundle WaitCompleted()
        {
            throw new Exception($"{nameof(LoadRemoteBundleTask)} can't wait completed.");
        }

        public override void Cancel()
        {
            _request?.Abort();
            base.Cancel();
        }

        protected override float GetDownloadPercent()
        {
            return _request?.downloadProgress ?? 0f;
        }

        protected override void OnExecute()
        {
            _request = UnityWebRequestAssetBundle.GetAssetBundle(new Uri(BundleUri));
            var op = _request.SendWebRequest();
            op.completed += OnBundleRequestCompleted;
        }

        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            if (_request.result != UnityWebRequest.Result.Success)
            {
                LLog.Error("Failed to download bundle : {0}", BundleUri);
                OnBundleLoaded(null);
            }
            else
            {
                var bundle = DownloadHandlerAssetBundle.GetContent(_request);
                OnBundleLoaded(bundle);
            }
            
            _request?.Dispose();
            _request = null;
        }
    }
}