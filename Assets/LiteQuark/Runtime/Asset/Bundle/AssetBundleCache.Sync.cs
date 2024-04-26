namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        private bool ForceLoadBundleComplete()
        {
            if (Stage == AssetCacheStage.Loaded)
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
            if (Stage == AssetCacheStage.Loaded)
            {
                return true;
            }

            if (Stage == AssetCacheStage.Loading)
            {
                return ForceLoadBundleComplete();
            }

            Stage = AssetCacheStage.Loading;
            var fullPath = PathUtils.GetFullPathInRuntime(Info.BundlePath);
            var bundle = UnityEngine.AssetBundle.LoadFromFile(fullPath);

            if (bundle != null)
            {
                OnBundleLoaded(bundle);
                return true;
            }
            else
            {
                Stage = AssetCacheStage.Invalid;
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