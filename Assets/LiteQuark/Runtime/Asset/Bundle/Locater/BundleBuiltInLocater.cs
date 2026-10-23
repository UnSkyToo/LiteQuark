namespace LiteQuark.Runtime
{
    internal class BundleBuiltInLocater : IBundleLocater
    {
        public bool EditorForceStreamingAssets { get; set; } = true;

        public BundleBuiltInLocater()
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

        public LoadVersionPackTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback)
        {
            var versionPackUri = GetRuntimeFullPath(versionFileName);
            return LiteRuntime.Task.AddLoadVersionPackTask(versionPackUri, callback);
        }

        public ILoadBundleTask LoadBundle(string bundlePath, string hash, System.Action<UnityEngine.AssetBundle> callback)
        {
            var bundleUri = GetRuntimeFullPath(bundlePath);
            return LiteRuntime.Task.AddLoadLocalBundleTask(bundleUri, callback);
        }
    }
}
