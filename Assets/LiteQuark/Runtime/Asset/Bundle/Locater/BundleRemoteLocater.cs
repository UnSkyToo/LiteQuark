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
            var versionPackUri = GetVersionPackUri(versionFileName);
            return LiteRuntime.Task.AddLoadVersionPackTask(versionPackUri, callback);
        }

        private string GetVersionPackUri(string versionFileName)
        {
            var uri = PathUtils.ConcatUri(_remoteUri, versionFileName);

            var fragmentIndex = uri.IndexOf('#');
            var fragment = fragmentIndex >= 0 ? uri.Substring(fragmentIndex) : string.Empty;

            if (fragmentIndex >= 0)
            {
                uri = uri.Substring(0, fragmentIndex);
            }

            var separator = uri.IndexOf('?') >= 0 ? "&" : "?";
            if (uri.EndsWith("?", System.StringComparison.Ordinal) ||
                uri.EndsWith("&", System.StringComparison.Ordinal))
            {
                separator = string.Empty;
            }

            var version = System.Uri.EscapeDataString(AppUtils.GetVersion());
            return $"{uri}{separator}t={version}{fragment}";
        }

        public ILoadBundleTask LoadBundle(string bundlePath, string hash, System.Action<UnityEngine.AssetBundle> callback)
        {
            var bundleUri = PathUtils.ConcatUri(_remoteUri, bundlePath);
            return LiteRuntime.Task.AddLoadRemoteBundleTask(bundleUri, DisableUnityWebCache ? null : hash, callback);
        }
    }
}
