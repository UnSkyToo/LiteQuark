using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public class UnityWebGetRequestTask : BaseTask
    {
        private readonly string Uri_;
        private Action<DownloadHandler> Callback_;
        private UnityWebRequest Request_;
        
        public UnityWebGetRequestTask(string uri, Action<DownloadHandler> callback)
            : base()
        {
            Uri_ = uri;
            Callback_ = callback;
        }

        public override void Dispose()
        {
            Callback_ = null;
        }

        protected override void OnExecute()
        {
            Request_ = UnityWebRequest.Get(Uri_);
            var asyncOperation = Request_.SendWebRequest();
            asyncOperation.completed += OnBundleRequestCompleted;
        }
        
        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            if (Request_.result != UnityWebRequest.Result.Success)
            {
                LLog.Error($"get request uri : {Uri_}\r\n{Request_.error}");
                Callback_?.Invoke(null);
            }
            else
            {
                Callback_?.Invoke(Request_.downloadHandler);
            }

            Stop();
        }
    }
}