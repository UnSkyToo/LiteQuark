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
        private AssetBundle _bundle;
        private Action<AssetBundle> _callback;
        
        public LoadRemoteBundleTask(string bundleUri, string hash, Action<AssetBundle> callback)
            : base(bundleUri, LiteRuntime.Setting.Asset.BundleDownloadRetry, false)
        {
            _bundleUri = bundleUri;
            _hash = hash;
            _callback = callback;
        }

        public override void Dispose()
        {
            base.Dispose();
            _callback = null;
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

            _bundle = null;
            Cancel();
            LiteUtils.SafeInvoke(_callback, null);
            LiteRuntime.FrameworkError(FrameworkErrorCode.LoadRemoteBundle, "Bundle download failed");
        }
        
        protected override void OnSuccess(UnityWebRequest request)
        {
            base.OnSuccess(request);
            
            _bundle = DownloadHandlerAssetBundle.GetContent(request);
            Complete(_bundle);
            LiteUtils.SafeInvoke(_callback, _bundle);
            LLog.Info("[LoadRemoteBundle] {0} : {1}KB", _bundleUri, request.downloadedBytes / 1024);
        }

        public AssetBundle GetBundle() => _bundle;
    }
}