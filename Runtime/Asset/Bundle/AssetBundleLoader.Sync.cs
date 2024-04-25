namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleLoader : IAssetLoader
    {
        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return null;
            }

            var isLoaded = LoadBundleSync(info);
            if (!isLoaded)
            {
                return null;
            }

            var cache = BundleCacheMap_[info.BundlePath];
            var asset = cache.LoadAssetSync<T>(assetPath);

            if (asset != null)
            {
                AssetIDToPathMap_.TryAdd(asset.GetInstanceID(), assetPath);
            }

            return asset;
        }

        private bool LoadBundleSync(BundleInfo info)
        {
            if (BundleExisted(info))
            {
                return true;
            }

            var isLoaded = LoadBundleCompleteSync(info);
            if (!isLoaded)
            {
                BundleCacheMap_.Remove(info.BundlePath);
            }

            return isLoaded;
        }

        private bool LoadBundleCompleteSync(BundleInfo info)
        {
            if (!BundleCacheMap_.TryGetValue(info.BundlePath, out var cache))
            {
                cache = new AssetBundleCache(info);
                BundleCacheMap_.Add(info.BundlePath, cache);
            }

            var isLoaded = LoadBundleDependenciesSync(cache);
            if (!isLoaded)
            {
                return false;
            }
            
            return cache.LoadBundleSync();
        }

        private bool LoadBundleDependenciesSync(AssetBundleCache cache)
        {
            var dependencies = cache.GetAllDependencies();
            if (dependencies == null || dependencies.Length == 0)
            {
                return true;
            }

            foreach (var dependency in dependencies)
            {
                var dependencyInfo = PackInfo_.GetBundleInfoFromBundlePath(dependency);
                var isLoaded = LoadBundleSync(dependencyInfo);
                if (!isLoaded)
                {
                    return false;
                }

                cache.AddDependencyCache(BundleCacheMap_[dependencyInfo.BundlePath]);
            }

            return true;
        }

        public UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent)
        {
            var asset = LoadAssetSync<UnityEngine.GameObject>(assetPath);
            var instance = UnityEngine.Object.Instantiate(asset, parent);
            if (instance != null)
            {
                AssetIDToPathMap_.TryAdd(instance.GetInstanceID(), assetPath);
            }
            return instance;
        }
    }
}