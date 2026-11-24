using System;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetManager
    {
        public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<T>>> callback) where T : UnityEngine.Object
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(new AssetError(assetKey, "Asset loading is closed")));
                return;
            }

            if (!_packInfo.TryGetBundleInfoByAssetKey(assetKey, out var bundleInfo))
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(new AssetError(assetKey, "Asset is not in the bundle manifest")));
                return;
            }

            var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
            cache.LoadAssetAsync(assetKey, priority, callback);
        }

        public void LoadSceneAsync(string sceneKey, AssetLoadPriority priority, Action<AssetLoadResult<IAssetLease<Scene?>>> callback)
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, "Scene loading is closed")));
                return;
            }

            if (!_packInfo.TryGetBundleInfoByAssetKey(sceneKey, out var bundleInfo))
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(sceneKey, "Scene is not in the bundle manifest")));
                return;
            }

            var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
            cache.LoadSceneAsync(sceneKey, priority, callback);
        }
    }
}
