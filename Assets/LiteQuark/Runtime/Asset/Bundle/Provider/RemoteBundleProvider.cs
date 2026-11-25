namespace LiteQuark.Runtime
{
    internal class RemoteBundleProvider : IBundleProvider
    {
        public bool DisableUnityWebCache { get; set; } = false;

        private readonly string _remoteUri;

        public RemoteBundleProvider(string remoteUri)
        {
            _remoteUri = remoteUri;
            LLog.Info("BundleRemoteUri: {0}", _remoteUri);
        }

        public BaseTask LoadVersionPack(string versionFileName)
        {
            var versionPackUri = GetVersionPackUri(versionFileName);
            var task = new LoadVersionPackTask(versionPackUri, null);
            LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return task;
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

            return $"{uri}{separator}t={System.Guid.NewGuid():N}{fragment}";
        }

        public ILoadBundleTask LoadBundle(string bundlePath, string hash)
        {
            var bundleUri = PathUtils.ConcatUri(_remoteUri, bundlePath);
            var task = new LoadRemoteBundleTask(bundleUri, DisableUnityWebCache ? null : hash);
            LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return task;
        }
    }
}
