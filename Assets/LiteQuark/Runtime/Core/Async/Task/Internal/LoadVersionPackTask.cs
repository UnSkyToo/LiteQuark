using System;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    internal sealed class LoadVersionPackTask : UnityDownloadBaseTask
    {
        private Action<VersionPackInfo> _callback;

        public LoadVersionPackTask(string uri, Action<VersionPackInfo> callback)
            : base(uri, new RetryParam(60, 3, 1f), true)
        {
            _callback = callback;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _callback = null;
        }

        protected override void OnExecute()
        {
            LLog.Info("Download VersionPackUri : {0}", Uri);
            base.OnExecute();
        }

        protected override void OnFailed()
        {
            base.OnFailed();

            NotifyCompleted(null);
            LiteRuntime.FrameworkError(FrameworkErrorCode.LoadVersionPack, Error.Message);
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            base.OnSuccess(request);

            try
            {
                var info = VersionPackInfo.FromBinaryData(request.downloadHandler.data);
                if (info is not { IsValid: true })
                {
                    FailVersionPack("VersionPack is invalid");
                    return;
                }

                info.Initialize();
                if (Complete(info))
                {
                    NotifyCompleted(info);
                }
            }
            catch (Exception ex)
            {
                FailVersionPack("VersionPack parse failed", ex);
            }
        }

        private void NotifyCompleted(VersionPackInfo info)
        {
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, info);
        }

        private void FailVersionPack(string message, Exception ex = null)
        {
            if (ex != null)
            {
                LLog.Exception(ex, message);
            }
            else
            {
                LLog.Error(message);
            }

            Fail(ex ?? new InvalidOperationException(message));
        }
    }
}
