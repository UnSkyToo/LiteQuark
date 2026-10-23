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

        protected override void OnDispose()
        {
            base.OnDispose();
            _callback = null;
        }

        protected override void OnFailed()
        {
            base.OnFailed();

            NotifyCompleted(null);
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            base.OnSuccess(request);

            var handler = request.downloadHandler;
            if (Complete(handler))
            {
                NotifyCompleted(handler);
            }
        }

        private void NotifyCompleted(DownloadHandler handler)
        {
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, handler);
        }
    }
}
