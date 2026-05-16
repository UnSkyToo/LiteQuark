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
            SetPriority(TaskPriority.Urgent);
        }

        public override void Dispose()
        {
            base.Dispose();
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
            
            Cancel();
            LiteUtils.SafeInvoke(_callback, null);
            LiteRuntime.FrameworkError(FrameworkErrorCode.LoadVersionPack, "VersionPack download failed");
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
                Complete(info);
                LiteUtils.SafeInvoke(_callback, info);
            }
            catch (Exception ex)
            {
                FailVersionPack("VersionPack parse failed", ex);
            }
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

            Cancel();
            LiteUtils.SafeInvoke(_callback, null);
            LiteRuntime.FrameworkError(FrameworkErrorCode.LoadVersionPack, message);
        }
    }
}
