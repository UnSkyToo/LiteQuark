using System;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LoadRemoteBundleTask : UnityDownloadBaseTask, ILoadBundleTask, IResourceTaskCleanup
    {
        public override string DebugName => $"LoadBundle {PathUtils.GetFileName(_bundleUri)}";

        private readonly string _bundleUri;
        private readonly string _hash;
        private Action<AssetBundle> _callback;
        private readonly UniTaskCompletionSource _cleanupCompletion = new();

        public UniTask WaitForCleanupAsync() => _cleanupCompletion.Task;

        public LoadRemoteBundleTask(string bundleUri, string hash, Action<AssetBundle> callback)
            : base(bundleUri, LiteRuntime.Setting.Asset.BundleDownloadRetry, false)
        {
            _bundleUri = bundleUri;
            _hash = hash;
            _callback = callback;
        }

        protected override void OnDispose()
        {
            _callback = null;
            base.OnDispose();
        }

        protected override void OnResourcesReleased(Exception error)
        {
            if (error == null)
            {
                _cleanupCompletion.TrySetResult();
            }
            else
            {
                _cleanupCompletion.TrySetException(error);
            }
        }

        protected override UnityWebRequest CreateRequest()
        {
            if (string.IsNullOrEmpty(_hash))
            {
                return UnityWebRequestAssetBundle.GetAssetBundle(_bundleUri);
            }

            return UnityWebRequestAssetBundle.GetAssetBundle(_bundleUri, Hash128.Parse(_hash));
        }

        protected override void OnFailed()
        {
            base.OnFailed();

            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, null);

            LiteRuntime.FrameworkError(FrameworkErrorCode.LoadRemoteBundle, "Bundle download failed");
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            base.OnSuccess(request);

            var bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (bundle == null)
            {
                Fail(new InvalidOperationException($"Bundle download returned no bundle: {_bundleUri}"));
                return;
            }

            var downloadedBytes = request.downloadedBytes;
            var callback = _callback;
            _callback = null;
            if (Complete(bundle))
            {
                LiteUtils.SafeInvoke(callback, bundle);
                LLog.Info("[LoadRemoteBundle] {0} : {1}KB", _bundleUri, downloadedBytes / 1024);
            }
        }

        public AssetBundle GetBundle() => Result as AssetBundle;
    }
}
