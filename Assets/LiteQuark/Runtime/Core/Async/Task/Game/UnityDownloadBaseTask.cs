using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public abstract class UnityDownloadBaseTask : BaseTask
    {
        public override string DebugName => $"Download {Uri}";
        
        protected readonly Uri Uri;
        private readonly int _timeout;
        private readonly bool _forceNoCache;
        private UnityWebRequest _request;
        private int _retryCount;
        
        protected UnityDownloadBaseTask(string uri, int timeout, int retryCount, bool forceNoCache)
            : base()
        {
            Uri = new Uri(uri);
            _timeout = Mathf.Max(timeout, 0);
            _retryCount = Math.Max(retryCount, 0);
            _forceNoCache = forceNoCache;
        }

        public override void Cancel()
        {
            _request?.Abort();
            base.Cancel();
        }
        
        protected override void OnTick(float deltaTime)
        {
            Progress = _request?.downloadProgress ?? 0f;
        }
        
        protected override void OnExecute()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                OnFailed();
                return;
            }
            
            StartDownload();
        }

        private void StartDownload()
        {
            _request = UnityWebRequest.Get(Uri);
            
            if (_forceNoCache)
            {
                _request.SetRequestHeader("Cache-Control", "no-cache");
                _request.SetRequestHeader("Pragma", "no-cache");
            }

            if (_timeout > 0)
            {
                _request.timeout = _timeout;
            }
            
            var asyncOperation = _request.SendWebRequest();
            asyncOperation.completed += OnRequestCompleted;
            
            LLog.Info("GET : {0}", Uri);
        }

        private void OnRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnRequestCompleted;
            
            var downloadHandler = _request.downloadHandler;
            
            if (_request.result != UnityWebRequest.Result.Success || !(downloadHandler?.isDone ?? false))
            {
                var error = _request.error;
                LLog.Error("UnityDownloadTask error : {0} - {1}\n{2}", Uri, _request.result, error);
                
                if (_retryCount > 0 && IsCanRetryError(error))
                {
                    _retryCount--;
                    LiteRuntime.Timer.AddTimer(1f, StartDownload);
                }
                else
                {
                    OnFailed();
                }
            }
            else
            {
                OnSuccess(_request);
            }
        }

        protected virtual void OnFailed()
        {
        }

        protected virtual void OnSuccess(UnityWebRequest request)
        {
        }

        public void AddRequestHeader(string name, string value)
        {
            _request?.SetRequestHeader(name, value);
        }
        
        private bool IsCanRetryError(string error)
        {
            return error.Contains("timeout") || error.Contains("Unknown Error") || error.Contains("connection");
        }
    }
}