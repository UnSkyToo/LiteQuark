namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public bool LoadBundleCompleteSync()
        {
            if (IsLoaded)
            {
                return true;
            }
            
            var info = Loader_.GetPackInfo().GetBundleInfoFromBundlePath(BundlePath_);
            if (info == null)
            {
                return false;
            }

            var isLoaded = LoadBundleDependenciesSync(info);
            if (!isLoaded)
            {
                return false;
            }
            
            return LoadBundleSync();
        }

        private bool LoadBundleDependenciesSync(BundleInfo info)
        {
            var dependencies = info.DependencyList;
            if (dependencies == null || dependencies.Length == 0)
            {
                return true;
            }

            foreach (var dependency in dependencies)
            {
                var dependencyCache = Loader_.GetOrCreateBundleCache(dependency);
                var isLoaded = dependencyCache.LoadBundleCompleteSync();
                if (!isLoaded)
                {
                    return false;
                }

                AddDependencyCache(dependencyCache);
            }

            return true;
        }
        
        private bool LoadBundleSync()
        {
            if (IsLoaded)
            {
                return true;
            }

            if (Stage == AssetCacheStage.Loading)
            {
                return ForceLoadBundleComplete();
            }

            Stage = AssetCacheStage.Loading;
            var fullPath = PathUtils.GetFullPathInRuntime(BundlePath_);
            var bundle = UnityEngine.AssetBundle.LoadFromFile(fullPath);

            if (bundle != null)
            {
                OnBundleLoaded(bundle);
                return true;
            }
            else
            {
                Stage = AssetCacheStage.Invalid;
                LLog.Error($"load bundle failed : {BundlePath_}");
                return false;
            }
        }
        
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