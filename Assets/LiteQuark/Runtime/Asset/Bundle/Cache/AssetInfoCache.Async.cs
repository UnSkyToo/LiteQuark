using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        public void LoadAssetAsync<T>(Lease<T> lease, Action<Lease<T>> callback) where T : UnityEngine.Object
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                lease.Dispose();
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            if (IsLoaded)
            {
                var success = lease.TryBindAsset(Asset);
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _assetType, _cache.BundlePath, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _assetType, _cache.BundlePath, success, errorMessage: success ? null : "Asset type mismatch", isCached: true);
                if (!success)
                {
                    lease.Dispose();
                    lease = null;
                }
                LiteUtils.SafeInvoke(callback, lease);
                return;
            }

            _assetLoaderCallbackList.Add(isLoaded =>
            {
                var success = isLoaded && lease.TryBindAsset(Asset);
                if (!success)
                {
                    lease.Dispose();
                    lease = null;
                }
                LiteUtils.SafeInvoke(callback, lease);
            });
            if (Stage != AssetCacheStage.Idle)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(_assetPath);

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _assetType, _cache.BundlePath);

            try
            {
                _loadAssetTask = LiteRuntime.Task.AddLoadAssetTask<T>(_cache.Bundle, name, HandleAssetLoadCompleted);
            }
            catch
            {
                HandleAssetLoadCompleted(null);
                throw;
            }
        }

        private void HandleAssetLoadCompleted(UnityEngine.Object asset)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                return;
            }

            _loadAssetTask = null;

            var isLoaded = OnAssetLoaded(asset);

            var callbacks = new System.Collections.Generic.List<Action<bool>>(_assetLoaderCallbackList);
            _assetLoaderCallbackList.Clear();

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _assetType, _cache.BundlePath, isLoaded, errorMessage: isLoaded ? null : "Asset load failed");

            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }
        }
    }
}
