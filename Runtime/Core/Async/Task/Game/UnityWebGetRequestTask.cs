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
            _callback = null;
        }

        protected override void OnFailed()
        {
            LiteUtils.SafeInvoke(_callback, null);
            Abort();
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            LiteUtils.SafeInvoke(_callback, request.downloadHandler);
            Complete(request.downloadHandler);
        }
    }
}