using System;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    internal sealed class LoadRemoteBundleTask : UnityDownloadBaseTask, ILoadBundleTask
    {
        public override string DebugName => $"LoadBundle {PathUtils.GetFileName(_bundleUri)}";

        private readonly string _bundleUri;
        private readonly string _hash;

        public LoadRemoteBundleTask(string bundleUri, string hash)
            : base(bundleUri, LiteRuntime.Setting.Asset.BundleDownloadRetry, false)
        {
            _bundleUri = bundleUri;
            _hash = hash;
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

            LiteRuntime.FrameworkError(FrameworkErrorCode.LoadRemoteBundle, "Bundle download failed");
        }

        protected override void OnSuccess(UnityWebRequest request)
        {
            if (IsDone)
            {
                return;
            }
            base.OnSuccess(request);

            var downloadedBytes = request.downloadedBytes;
            var bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (bundle == null)
            {
                throw new InvalidOperationException($"Bundle download returned no bundle: {_bundleUri}");
            }

            var resource = new AssetBundleResource(bundle);
            if (Complete(resource))
            {
                LLog.Info("[LoadRemoteBundle] {0} : {1}KB", _bundleUri, downloadedBytes / 1024);
            }
            else
            {
                resource.Dispose();
            }
        }

        public IBundleResource GetBundle() => Result as IBundleResource;
    }
}
