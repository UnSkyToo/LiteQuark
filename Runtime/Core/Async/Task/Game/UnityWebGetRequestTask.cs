using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class UnityWebGetRequestTask : BaseTask
    {
        private readonly Uri _uri;
        private readonly int _timeout;
        private readonly bool _forceNoCache;
        private Action<DownloadHandler> _callback;
        private UnityWebRequest _request;
        
        public UnityWebGetRequestTask(string uri, int timeout, bool forceNoCache, Action<DownloadHandler> callback)
            : base()
        {
            _uri = new Uri(uri);
            _timeout = timeout;
            _forceNoCache = forceNoCache;
            _callback = callback;
        }

        public override void Dispose()
        {
            _callback = null;
        }

        protected override void OnExecute()
        {
            _request = UnityWebRequest.Get(_uri);
            if (_forceNoCache)
            {
                _request.SetRequestHeader("Cache-Control", "no-cache");
            }

            _request.timeout = _timeout;
            var asyncOperation = _request.SendWebRequest();
            asyncOperation.completed += OnBundleRequestCompleted;
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _request?.downloadProgress ?? 0f;
        }

        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            if (_request.result != UnityWebRequest.Result.Success)
            {
                LLog.Error($"get request uri : {_uri}\n{_request.error}");
                _callback?.Invoke(null);
                Abort();
            }
            else
            {
                _callback?.Invoke(_request.downloadHandler);
                Complete(_request.downloadedBytes);
            }
        }
    }
}