using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            if (!_packInfo.TryGetBundleInfoByAssetKey(assetKey, out var bundleInfo))
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
            cache.LoadAssetAsync(assetKey, priority, callback);
        }

        public void LoadSceneAsync(string sceneKey, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority, Action<SceneInfoCache.Lease> callback)
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            if (!_loadingSceneSet.Add(sceneKey))
            {
                LLog.Error("Scene is already loading: {0}", sceneKey);
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            try
            {
                if (!_packInfo.TryGetBundleInfoByAssetKey(sceneKey, out var bundleInfo))
                {
                    _loadingSceneSet.Remove(sceneKey);
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }

                var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
                cache.LoadSceneAsync(sceneKey, parameters, priority, lease =>
                {
                    _loadingSceneSet.Remove(sceneKey);
                    LiteUtils.SafeInvoke(callback, lease);
                });
            }
            catch
            {
                _loadingSceneSet.Remove(sceneKey);
                throw;
            }
        }
    }
}
