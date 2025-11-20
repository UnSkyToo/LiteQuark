using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var bundleInfo = _packInfo.GetBundleInfoFromAssetPath(assetPath);
            if (bundleInfo == null)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
            cache.LoadAssetAsync<T>(assetPath, (asset) =>
            {
                UpdateAssetIDToPathMap(asset, assetPath);
                LiteUtils.SafeInvoke(callback, asset);
            });
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                UpdateAssetIDToPathMap(instance, assetPath);
                LiteUtils.SafeInvoke(callback, instance);
            });
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset, position, rotation, parent);
                UpdateAssetIDToPathMap(instance, assetPath);
                LiteUtils.SafeInvoke(callback, instance);
            });
        }

        public void LoadSceneAsync(string scenePath, string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var bundleInfo = _packInfo.GetBundleInfoFromAssetPath(scenePath);
            if (bundleInfo == null)
            {
                LiteUtils.SafeInvoke(callback, false);
                return;
            }

            var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
            cache.LoadSceneAsync(sceneName, parameters, (result) =>
            {
                LiteUtils.SafeInvoke(callback, result);
            });
        }
    }
}