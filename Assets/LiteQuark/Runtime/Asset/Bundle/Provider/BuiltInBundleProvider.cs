namespace LiteQuark.Runtime
{
    internal class BuiltInBundleProvider : IBundleProvider
    {
        public bool EditorForceStreamingAssets { get; set; } = true;

        public BuiltInBundleProvider()
        {
        }

        private string GetRuntimeFullPath(string path)
        {
#if UNITY_EDITOR
            if (EditorForceStreamingAssets)
            {
                return PathUtils.GetStreamingAssetsPath(LiteConst.Tag, path);
            }
#endif
            var persistentPath = PathUtils.GetPersistentDataPath(LiteConst.Tag, path);
            if (System.IO.File.Exists(persistentPath))
            {
                return persistentPath;
            }

            return PathUtils.GetStreamingAssetsPath(LiteConst.Tag, path);
        }

        public BaseTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback)
        {
            var versionPackUri = GetRuntimeFullPath(versionFileName);
            return LiteRuntime.Task.AddLoadVersionPackTask(versionPackUri, callback);
        }

        public BaseTask LoadBundle(string bundlePath, string hash, System.Action<IBundleResource> callback)
        {
            var bundleUri = GetRuntimeFullPath(bundlePath);
            return LiteRuntime.Task.AddLoadLocalBundleTask(bundleUri,
                bundle => callback?.Invoke(bundle != null ? new AssetBundleResource(bundle) : null));
        }
    }
}
