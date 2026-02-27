namespace LiteQuark.Runtime
{
    internal class BundleRemoteLocater : IBundleLocater
    {
        public bool DisableUnityWebCache { get; set; } = false;
        
        private readonly string _remoteUri;
        
        public BundleRemoteLocater(string remoteUri)
        {
            _remoteUri = remoteUri;
            LLog.Info("BundleRemoteUri: {0}", _remoteUri);
        }

        public LoadVersionPackTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback)
        {
            var versionPackUri = PathUtils.ConcatPath(_remoteUri, versionFileName);
            versionPackUri = $"{versionPackUri}?t={AppUtils.GetVersion()}";
            return LiteRuntime.Task.AddLoadVersionPackTask(versionPackUri, callback);
        }
        
        public ILoadBundleTask LoadBundle(string bundlePath, string hash, System.Action<UnityEngine.AssetBundle> callback)
        {
            var bundleUri = PathUtils.ConcatPath(_remoteUri, bundlePath);
            return LiteRuntime.Task.AddLoadRemoteBundleTask(bundleUri, DisableUnityWebCache ? null : hash, callback);
        }
    }
}