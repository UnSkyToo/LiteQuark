using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class LoadVersionPackTask : BaseTask
    {
        private readonly Uri _uri;
        private Action<VersionPackInfo> _callback;
        private UnityWebRequest _request;
        
        public LoadVersionPackTask(string uri, Action<VersionPackInfo> callback)
            : base()
        {
            _uri = new Uri(uri);
            _callback = callback;
        }

        public override void Dispose()
        {
            _callback = null;
        }

        protected override void OnExecute()
        {
            _request = UnityWebRequest.Get(_uri);
            _request.SetRequestHeader("Cache-Control", "no-cache");
            _request.timeout = 0;
            var asyncOperation = _request.SendWebRequest();
            asyncOperation.completed += OnRequestCompleted;
            
            LLog.Info("VersionPackUri : {0}", _uri);
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _request?.downloadProgress ?? 0f;
        }

        private void OnRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnRequestCompleted;
            
            var downloadHandler = _request.downloadHandler;
            
            if (_request.result != UnityWebRequest.Result.Success || !(downloadHandler?.isDone ?? false))
            {
                LLog.Error("LoadVersionPackTask error : {0} - {1}\n{2}", _uri, _request.result, _request.error);
                _callback?.Invoke(null);
                Abort();
            }
            else
            {
                var info = VersionPackInfo.FromBinaryData(downloadHandler.data);
                if (info is not { IsValid: true })
                {
                    LLog.Error("Bundle package parse error\n{0}", downloadHandler.error);
                    _callback?.Invoke(null);
                    Abort();
                }
                else
                {
                    info.Initialize();
                    _callback?.Invoke(info);
                    Complete(info);
                }
            }
        }
    }
}