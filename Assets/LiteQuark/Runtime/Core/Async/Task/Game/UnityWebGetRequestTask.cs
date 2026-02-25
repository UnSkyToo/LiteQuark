using System;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class UnityWebGetRequestTask : UnityDownloadBaseTask
    {
        private Action<DownloadHandler> _callback;
        
        public UnityWebGetRequestTask(string uri, RetryParam retry, bool forceNoCache, Action<DownloadHandler> callback)
            : base(uri, retry, forceNoCache)
        {
            _callback = callback;
        }

        public override void Dispose()
        {
            base.Dispose();
            _callback = null;
        }

        protected override void OnFailed()
        {
            base.OnFailed();
            
            Abort();
            LiteUtils.SafeInvoke(_callback, null);
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            base.OnSuccess(request);
            
            var handler = request.downloadHandler;
            Complete(handler);
            LiteUtils.SafeInvoke(_callback, handler);
        }
    }
}