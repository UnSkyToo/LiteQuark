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
        internal bool IsDelivering { get; private set; }

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

        protected override void OnDispose()
        {
            StopRetry();
            if (!IsDelivering)
            {
                _request?.Dispose();
                _request = null;
            }
        }

        protected override void OnTerminated()
        {
            StopRetry();
            try
            {
                if (State == TaskState.Failed)
                {
                    OnFailed();
                }
            }
            finally
            {
                if (!IsDelivering)
                {
                    FinishRequest();
                }
            }
        }

        private void StopRetry()
        {
            if (_retryTimerID != 0)
            {
                LiteRuntime.Timer.StopTimer(_retryTimerID);
                _retryTimerID = 0;
            }
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _request?.downloadProgress ?? 0f;
        }

        protected override void OnExecute()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Fail(new InvalidOperationException($"Network is not reachable: {Uri}"));
                return;
            }

            StartDownload();
        }

        private void StartDownload()
        {
            _retryTimerID = 0;

            if (IsDone)
            {
                return;
            }

            try
            {
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
            catch (Exception ex)
            {
                FailRequest(ex);
            }
        }

        protected virtual UnityWebRequest CreateRequest()
        {
            return UnityWebRequest.Get(Uri);
        }

        private void OnRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnRequestCompleted;

            if (IsDone || _request == null)
            {
                return;
            }

            IsDelivering = true;
            try
            {
                var downloadHandler = _request.downloadHandler;
                if (_request.result != UnityWebRequest.Result.Success || !(downloadHandler?.isDone ?? false))
                {
                    var error = _request.error;
                    var message = $"{GetType().Name} failed: {Uri}, {_request.result}, HTTP {_request.responseCode}, {error}";
                    LLog.Error(message);

                    if (_retryCount > 0 && RetryParam.IsCanRetryError(error ?? string.Empty))
                    {
                        _retryCount--;
                        _retryTimerID = LiteRuntime.Timer.AddTimer(_retryInterval, StartDownload);
                    }
                    else
                    {
                        FailRequest(new InvalidOperationException(message));
                    }
                }
                else
                {
                    OnSuccess(_request);
                }
            }
            catch (Exception ex)
            {
                if (IsDone)
                {
                    LLog.Exception(ex);
                }
                else
                {
                    FailRequest(ex);
                }
            }
            finally
            {
                FinishRequest();
            }
        }

        internal void ReleaseRequest(bool abort)
        {
            var request = _request;
            _request = null;
            using (request)
            {
                if (abort) request?.Abort();
            }
        }

        private void FailRequest(Exception error)
        {
            try
            {
                ReleaseRequest(abort: true);
            }
            catch (Exception ex)
            {
                error = new AggregateException(error, ex);
            }
            Fail(error);
        }

        private void FinishRequest()
        {
            Exception releaseError = null;
            try
            {
                ReleaseRequest(State is TaskState.Cancelled or TaskState.Failed);
            }
            catch (Exception ex)
            {
                releaseError = ex;
                if (!IsDone)
                {
                    Fail(ex);
                }
            }

            IsDelivering = false;
            if (IsDone)
            {
                OnResourcesReleased(releaseError);
            }
        }

        protected virtual void OnResourcesReleased(Exception error)
        {
            if (error != null)
            {
                LLog.Exception(error, "Release download request failed : {0}", Uri);
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
