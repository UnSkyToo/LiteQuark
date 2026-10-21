using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        public void LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var bundleInfo = _packInfo.GetBundleInfoFromAssetPath(assetPath);
            if (bundleInfo == null)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
            cache.LoadAssetAsync(assetPath, priority, callback);
        }

        public void LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority, Action<SceneInfoCache.Lease> callback)
        {
            if (_isDisposed)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            if (!_loadingSceneSet.Add(sceneName))
            {
                LLog.Error("Scene is already loading: {0}", sceneName);
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var formatPath = PathUtils.FormatAssetPath(scenePath);

            try
            {
                var bundleInfo = _packInfo.GetBundleInfoFromAssetPath(formatPath);
                if (bundleInfo == null)
                {
                    _loadingSceneSet.Remove(sceneName);
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }

                var cache = GetOrCreateBundleCache(bundleInfo.BundlePath);
                cache.LoadSceneAsync(sceneName, parameters, priority, lease =>
                {
                    _loadingSceneSet.Remove(sceneName);
                    LiteUtils.SafeInvoke(callback, lease);
                });
            }
            catch
            {
                _loadingSceneSet.Remove(sceneName);
                throw;
            }
        }
    }
}
