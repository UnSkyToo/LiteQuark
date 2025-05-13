using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var info = _packInfo.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                callback?.Invoke(null);
                return;
            }

            var cache = GetOrCreateBundleCache(info.BundlePath);
            cache.LoadBundleAsync((isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                cache.LoadAssetAsync<T>(assetPath, (asset) =>
                {
                    UpdateAssetIDToPathMap(asset, assetPath);
                    callback?.Invoke(asset);
                });
            });
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                UpdateAssetIDToPathMap(instance, assetPath);
                callback?.Invoke(instance);
            });
        }

        public void LoadSceneAsync(string scenePath, string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var info = _packInfo.GetBundleInfoFromAssetPath(scenePath);
            if (info == null)
            {
                callback?.Invoke(false);
                return;
            }

            var cache = GetOrCreateBundleCache(info.BundlePath);
            cache.LoadBundleAsync((isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(false);
                    return;
                }
                
                cache.LoadSceneAsync(sceneName, parameters, (result) =>
                {
                    callback?.Invoke(result);
                });
            });
        }
    }
}