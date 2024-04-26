using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleLoader : IAssetLoader
    {
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                callback?.Invoke(null);
                return;
            }

            var cache = GetOrCreateBundleCache(info.BundlePath);
            cache.LoadBundleCompleteAsync((isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                cache.LoadAssetAsync<T>(assetPath, (asset) =>
                {
                    if (asset != null)
                    {
                        AssetIDToPathMap_.TryAdd(asset.GetInstanceID(), assetPath);
                    }
                    callback?.Invoke(asset);
                });
            });
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                if (instance != null)
                {
                    AssetIDToPathMap_.TryAdd(instance.GetInstanceID(), assetPath);
                }
                callback?.Invoke(instance);
            });
        }
    }
}