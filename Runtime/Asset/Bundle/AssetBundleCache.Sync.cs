namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : IDispose
    {
        private bool ForceLoadBundleComplete()
        {
            if (IsLoaded)
            {
                return true;
            }

            foreach (var bundle in DependencyCacheList_)
            {
                if (!bundle.ForceLoadBundleComplete())
                {
                    return false;
                }
            }

            if (BundleRequest_ != null)
            {
                var bundle = BundleRequest_.assetBundle;
                return bundle != null;
            }

            return true;
        }
        
        public bool LoadBundleSync()
        {
            if (IsLoaded)
            {
                return true;
            }

            if (BundleRequest_ != null)
            {
                return ForceLoadBundleComplete();
            }
            
            var fullPath = PathUtils.GetFullPathInRuntime(Info.BundlePath);
            BeginLoadTime_ = UnityEngine.Time.realtimeSinceStartupAsDouble;
            var bundle = UnityEngine.AssetBundle.LoadFromFile(fullPath);

            if (bundle != null)
            {
                OnBundleLoaded(bundle);
                return true;
            }
            else
            {
                LLog.Error($"load asset bundle failed : {Info.BundlePath}");
                return false;
            }
        }

        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var cache = GetOrCreateAssetCache(assetPath);
            var asset = cache.LoadAssetSync<T>();
            
            if (asset != null)
            {
                IncRef();
            }
            
            return asset;
        }
    }
}