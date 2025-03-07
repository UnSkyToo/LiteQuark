using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class UnityWebGetRequestTask : BaseTask
    {
        private readonly Uri Uri_;
        private readonly int Timeout_;
        private readonly bool ForceNoCache_;
        private Action<DownloadHandler> Callback_;
        private UnityWebRequest Request_;
        
        public UnityWebGetRequestTask(string uri, int timeout, bool forceNoCache, Action<DownloadHandler> callback)
            : base()
        {
            Uri_ = new Uri(uri);
            Timeout_ = timeout;
            ForceNoCache_ = forceNoCache;
            Callback_ = callback;
        }

        public override void Dispose()
        {
            Callback_ = null;
        }

        protected override void OnExecute()
        {
            Request_ = UnityWebRequest.Get(Uri_);
            if (ForceNoCache_)
            {
                Request_.SetRequestHeader("Cache-Control", "no-cache");
            }

            Request_.timeout = Timeout_;
            var asyncOperation = Request_.SendWebRequest();
            asyncOperation.completed += OnBundleRequestCompleted;
        }
        
        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            if (Request_.result != UnityWebRequest.Result.Success)
            {
                LLog.Error($"get request uri : {Uri_}\n{Request_.error}");
                Callback_?.Invoke(null);
                Abort();
            }
            else
            {
                Callback_?.Invoke(Request_.downloadHandler);
                Complete();
            }
        }
    }
}