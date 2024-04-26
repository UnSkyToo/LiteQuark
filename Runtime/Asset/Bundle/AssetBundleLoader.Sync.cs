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

            var cache = GetOrCreateBundleCache(info.BundlePath);
            
            var isLoaded = cache.LoadBundleCompleteSync();
            if (!isLoaded)
            {
                return null;
            }

            var asset = cache.LoadAssetSync<T>(assetPath);
            if (asset != null)
            {
                AssetIDToPathMap_.TryAdd(asset.GetInstanceID(), assetPath);
            }

            return asset;
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