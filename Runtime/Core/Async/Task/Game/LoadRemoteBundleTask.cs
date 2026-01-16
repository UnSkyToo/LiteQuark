using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class LoadRemoteBundleTask : LoadBundleBaseTask
    {
        private UnityWebRequest _request;
        private int _timeout;
        private int _retryCount;
        
        public LoadRemoteBundleTask(string bundleUri, Action<AssetBundle> callback)
            : base(bundleUri, callback)
        {
        }

        public override void Dispose()
        {
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }
            
            base.Dispose();
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
            _retryCount = Mathf.Max(LiteRuntime.Setting.Asset.BundleDownloadMaxRetries, 0);
            _timeout = Mathf.Max(LiteRuntime.Setting.Asset.BundleDownloadTimeout, 0);
            StartDownload();
        }

        private void StartDownload()
        {
            _request = UnityWebRequestAssetBundle.GetAssetBundle(new Uri(BundleUri));
            if (_timeout > 0)
            {
                _request.timeout = _timeout;
            }
            var op = _request.SendWebRequest();
            op.completed += OnBundleRequestCompleted;
        }

        private void OnBundleRequestCompleted(AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;
            
            if (_request.result != UnityWebRequest.Result.Success)
            {
                var error = _request.error;
                LLog.Error("Failed to download bundle : {0}\n{1}", BundleUri, error);

                if (_retryCount > 0 && IsCanRetryError(error))
                {
                    _retryCount--;
                    LiteRuntime.Timer.AddTimer(1f, StartDownload);
                }
                else
                {
                    OnBundleLoaded(null);
                    LiteRuntime.FrameworkError(FrameworkErrorCode.NetError, error);
                }
            }
            else
            {
                var bundle = DownloadHandlerAssetBundle.GetContent(_request);
                OnBundleLoaded(bundle);
                LLog.Info("[LoadRemoteBundle] {0} : {1}KB", BundleUri, _request.downloadedBytes / 1024);
            }
            
            _request?.Dispose();
            _request = null;
        }

        private bool IsCanRetryError(string error)
        {
            return error.Contains("timeout") || error.Contains("Unknown Error") || error.Contains("connection");
        }
    }
}