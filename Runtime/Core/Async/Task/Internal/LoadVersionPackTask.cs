using System;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    internal sealed class LoadVersionPackTask : UnityDownloadBaseTask
    {
        private Action<VersionPackInfo> _callback;
        
        public LoadVersionPackTask(string uri, Action<VersionPackInfo> callback)
            : base(uri, 60, 3, true)
        {
            _callback = callback;
            SetPriority(TaskPriority.Urgent);
        }

        public override void Dispose()
        {
            _callback = null;
        }
        
        protected override void OnExecute()
        {
            LLog.Info("Download VersionPackUri : {0}", Uri);
            base.OnExecute();
        }

        protected override void OnFailed()
        {
            LiteUtils.SafeInvoke(_callback, null);
            Abort();
            LiteRuntime.Event.Send(new FrameworkErrorEvent(FrameworkErrorCode.NetError, "VersionPack download failed"));
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            var info = VersionPackInfo.FromBinaryData(request.downloadHandler.data);
            if (info is not { IsValid: true })
            {
                var error = request.downloadHandler.error;
                LLog.Error("Bundle package parse error\n{0}", error);
                LiteUtils.SafeInvoke(_callback, null);
                Abort();
            }
            else
            {
                info.Initialize();
                LiteUtils.SafeInvoke(_callback, info);
                Complete(info);
            }
        }
    }
}