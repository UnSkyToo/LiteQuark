using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal class BundleRemoteLocater : IBundleLocater
    {
        private readonly string _remoteUri;
        
        public BundleRemoteLocater(string remoteUri)
        {
            _remoteUri = remoteUri.ToLower();
            LLog.Info("BundleRemoteUri: {0}", _remoteUri);
        }

        public UniTask<VersionPackInfo> LoadVersionPack(string versionFileName)
        {
            var versionPackUri = PathUtils.ConcatPath(_remoteUri, versionFileName);
            LLog.Info("VersionPackUri : {0}", versionPackUri);
            return VersionPackInfo.LoadPackAsync(versionPackUri);
        }

        public LoadBundleBaseTask LoadBundle(string bundlePath, System.Action<UnityEngine.AssetBundle> callback)
        {
            var bundleUri = PathUtils.ConcatPath(_remoteUri, bundlePath);
            return LiteRuntime.Task.LoadRemoteBundleTask(bundleUri, callback);
        }
    }
}