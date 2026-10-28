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
        private bool _isDelivering;
        private bool _isReleased;
        private Exception _cleanupError;

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
            ReleaseResources();
        }

        protected override void OnTerminated()
        {
            base.OnTerminated();

            try
            {
                try
                {
                    if (State is TaskState.Failed or TaskState.Cancelled)
                    {
                        _request?.Abort();
                    }
                }
                finally
                {
                    if (State == TaskState.Failed)
                    {
                        OnFailed();
                    }
                }
            }
            finally
            {
                // 终态不依赖调度器回收包装器，能释放的资源立即释放。
                ReleaseResources();
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
                Fail(ex);
            }
        }

        protected virtual UnityWebRequest CreateRequest()
        {
            return UnityWebRequest.Get(Uri);
        }

        private void OnRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnRequestCompleted;

            if (_request == null)
            {
                return;
            }

            _isDelivering = true;
            try
            {
                if (IsDone)
                {
                    return;
                }

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
                        Fail(new InvalidOperationException(message));
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
                    Fail(ex);
                }
            }
            finally
            {
                _isDelivering = false;
                if (IsDone)
                {
                    ReleaseResources();
                }
                else
                {
                    // 本轮请求结束，但重试计时器仍持有下一次尝试。
                    ReleaseRequest();
                    if (_cleanupError != null)
                    {
                        // 先保存释放错误，再终结任务；终态清理会停止已安排的重试。
                        Fail(_cleanupError);
                    }
                }
            }
        }

        private void ReleaseResources()
        {
            // 结果通知期间不释放当前请求，交由完成回调的 finally 处理。
            if (_isDelivering || _isReleased)
            {
                return;
            }

            if (_retryTimerID != 0)
            {
                LiteRuntime.Timer.StopTimer(_retryTimerID);
                _retryTimerID = 0;
            }

            ReleaseRequest();

            if (!IsDone)
            {
                return;
            }

            _isReleased = true;
            OnResourcesReleased(_cleanupError);
        }

        private void ReleaseRequest()
        {
            var request = _request;
            _request = null;
            try
            {
                request?.Dispose();
            }
            catch (Exception ex)
            {
                _cleanupError ??= ex;
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
