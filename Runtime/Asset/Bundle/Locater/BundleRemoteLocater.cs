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

        public LoadVersionPackTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback)
        {
            var versionPackUri = PathUtils.ConcatPath(_remoteUri, versionFileName);
            versionPackUri = $"{versionPackUri}?t={AppUtils.GetVersion()}";
            return LiteRuntime.Task.LoadVersionPackTask(versionPackUri, callback);
        }

        public LoadBundleBaseTask LoadBundle(string bundlePath, System.Action<UnityEngine.AssetBundle> callback)
        {
            var bundleUri = PathUtils.ConcatPath(_remoteUri, bundlePath);
            return LiteRuntime.Task.LoadRemoteBundleTask(bundleUri, callback);
        }
    }
}