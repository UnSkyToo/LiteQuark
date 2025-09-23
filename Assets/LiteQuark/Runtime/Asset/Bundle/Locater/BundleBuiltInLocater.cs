using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal class BundleBuiltInLocater : IBundleLocater
    {
        private readonly bool _editorForceStreamingAssets;
        
        public BundleBuiltInLocater()
        {
            _editorForceStreamingAssets = LiteRuntime.Setting.Asset.EditorForceStreamingAssets;
        }

        private string GetRuntimeFullPath(string path)
        {
            var uri = PathUtils.GetFullPathInRuntime(path);
                
#if UNITY_EDITOR
            if (_editorForceStreamingAssets)
            {
                uri = PathUtils.GetStreamingAssetsPath(LiteConst.Tag, path);
            }
#endif

            return uri;
        }
        
        public UniTask<VersionPackInfo> LoadVersionPack(string versionFileName)
        {
            var versionPackUri = GetRuntimeFullPath(versionFileName);
            LLog.Info($"VersionPackUri : {versionPackUri}");
            return VersionPackInfo.LoadPackAsync(versionPackUri);
        }
        
        public LoadBundleBaseTask LoadBundle(string bundlePath, System.Action<UnityEngine.AssetBundle> callback)
        {
            var bundleUri = GetRuntimeFullPath(bundlePath);
            return LiteRuntime.Task.LoadLocalBundleTask(bundleUri, callback);
        }
    }
}