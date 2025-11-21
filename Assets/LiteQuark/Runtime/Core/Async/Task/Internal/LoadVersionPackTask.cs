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
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            var info = VersionPackInfo.FromBinaryData(request.downloadHandler.data);
            if (info is not { IsValid: true })
            {
                LLog.Error("Bundle package parse error\n{0}", request.downloadHandler.error);
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