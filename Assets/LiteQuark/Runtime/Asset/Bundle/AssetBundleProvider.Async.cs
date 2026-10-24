using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetLoadResult<AssetInfoCache.Lease<T>>> callback) where T : UnityEngine.Object
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<AssetInfoCache.Lease<T>>.Failure(new AssetError(assetKey, "Asset provider is disposed")));
                return;
            }

            if (!_packInfo.TryGetBundleInfoByAssetKey(assetKey, out var bundleInfo))
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<AssetInfoCache.Lease<T>>.Failure(new AssetError(assetKey, "Asset is not in the bundle manifest")));
                return;
            }

            var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
            cache.LoadAssetAsync(assetKey, priority, callback);
        }

        public void LoadSceneAsync(string sceneKey, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority, Action<AssetLoadResult<SceneInfoCache.Lease>> callback)
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<SceneInfoCache.Lease>.Failure(new AssetError(sceneKey, "Asset provider is disposed")));
                return;
            }

            if (!_loadingSceneSet.Add(sceneKey))
            {
                LLog.Error("Scene is already loading: {0}", sceneKey);
                LiteUtils.SafeInvoke(callback, AssetLoadResult<SceneInfoCache.Lease>.Failure(new AssetError(sceneKey, "Scene is already loading")));
                return;
            }

            try
            {
                if (!_packInfo.TryGetBundleInfoByAssetKey(sceneKey, out var bundleInfo))
                {
                    _loadingSceneSet.Remove(sceneKey);
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<SceneInfoCache.Lease>.Failure(new AssetError(sceneKey, "Scene is not in the bundle manifest")));
                    return;
                }

                var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
                cache.LoadSceneAsync(sceneKey, parameters, priority, result =>
                {
                    _loadingSceneSet.Remove(sceneKey);
                    LiteUtils.SafeInvoke(callback, result);
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
