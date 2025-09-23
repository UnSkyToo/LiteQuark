using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal class BundleRemoteLocater : IBundleLocater
    {
        private readonly string _remoteUri;
        
        public BundleRemoteLocater()
        {
            _remoteUri = PathUtils.ConcatPath(
                LiteRuntime.Setting.Asset.BundleRemoteUri,
                AppUtils.GetCurrentPlatform(),
                AppUtils.GetVersion()).ToLower();
            LLog.Info($"BundleRemoteUri: {_remoteUri}");
        }

        public UniTask<VersionPackInfo> LoadVersionPack(string versionFileName)
        {
            var versionPackUri = PathUtils.ConcatPath(_remoteUri, versionFileName);
            LLog.Info($"VersionPackUri : {versionPackUri}");
            return VersionPackInfo.LoadPackAsync(versionPackUri);
        }

        public LoadBundleBaseTask LoadBundle(string bundlePath, System.Action<UnityEngine.AssetBundle> callback)
        {
            var bundleUri = PathUtils.ConcatPath(_remoteUri, bundlePath);
            return LiteRuntime.Task.LoadRemoteBundleTask(bundleUri, callback);
        }
    }
}