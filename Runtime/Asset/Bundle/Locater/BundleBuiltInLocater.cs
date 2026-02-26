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
            var uri = PathUtils.GetFullPathInRuntime(path);
                
#if UNITY_EDITOR
            if (EditorForceStreamingAssets)
            {
                uri = PathUtils.GetStreamingAssetsPath(LiteConst.Tag, path);
            }
#endif

            return uri;
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