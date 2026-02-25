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
        private readonly float _retryInterval;
        private readonly bool _forceNoCache;
        private UnityWebRequest _request;
        private int _retryCount;
        private ulong _retryTimerID;
        
        protected UnityDownloadBaseTask(string uri, RetryParam retry, bool forceNoCache)
            : base()
        {
            Uri = new Uri(uri);
            _timeout = Mathf.Max(retry.Timeout, 0);
            _retryCount = Math.Max(retry.MaxRetries, 0);
            _retryInterval = Mathf.Max(retry.RetryInterval, 0.01f);
            _forceNoCache = forceNoCache;
            _retryTimerID = 0;
        }

        public override void Dispose()
        {
            if (_retryTimerID != 0)
            {
                LiteRuntime.Timer.StopTimer(_retryTimerID);
                _retryTimerID = 0;
            }
            
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }
        }

        public override void Cancel()
        {
            base.Cancel();
            _request?.Abort();
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
            _retryTimerID = 0;
            _request = CreateRequest();
            
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
        
        protected virtual UnityWebRequest CreateRequest()
        {
            return UnityWebRequest.Get(Uri);
        }

        private void OnRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnRequestCompleted;
            
            if (IsDone)
            {
                return;
            }
            
            if (_request == null)
            {
                return;
            }
            
            var downloadHandler = _request.downloadHandler;
            if (_request.result != UnityWebRequest.Result.Success || !(downloadHandler?.isDone ?? false))
            {
                var error = _request.error;
                LLog.Error("{0} error : {1} - {2}\n{3}", GetType().Name, Uri, _request.result, error);
                
                if (_retryCount > 0 && RetryParam.IsCanRetryError(error))
                {
                    _retryCount--;
                    _request?.Dispose();
                    _request = null;
                    _retryTimerID = LiteRuntime.Timer.AddTimer(_retryInterval, StartDownload);
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
    }
}