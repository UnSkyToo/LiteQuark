using System;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class UnityWebGetRequestTask : UnityDownloadBaseTask
    {
        private Action<byte[]> _callback;

        public UnityWebGetRequestTask(string uri, RetryParam retry, bool forceNoCache, Action<byte[]> callback)
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

            if (IsDone)
            {
                return;
            }

            var handler = request.downloadHandler ?? throw new InvalidOperationException("Download handler is missing.");
            var data = handler.data ?? Array.Empty<byte>();
            if (Complete(data))
            {
                NotifyCompleted(data);
            }
        }

        private void NotifyCompleted(byte[] data)
        {
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, data);
        }
    }
}
