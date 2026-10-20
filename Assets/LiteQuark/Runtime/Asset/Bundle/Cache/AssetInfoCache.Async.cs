using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(Action<Lease<T>> callback) where T : UnityEngine.Object
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            if (IsLoaded)
            {
                var lease = TryAcquireLease<T>();
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath, lease != null,
                    errorMessage: lease != null ? null : "Asset type mismatch", isCached: true);
                LiteUtils.SafeInvoke(callback, lease);
                return;
            }

            _assetLoaderCallbackList.Add(isLoaded =>
            {
                var lease = isLoaded ? TryAcquireLease<T>() : null;
                LiteUtils.SafeInvoke(callback, lease);
            });
            if (Stage != AssetCacheStage.Created)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(_assetPath);

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath);

            try
            {
                _loadAssetTask = LiteRuntime.Task.AddLoadAssetTask<T>(_bundleLease.Bundle, name, HandleAssetLoadCompleted);
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

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath, isLoaded, errorMessage: isLoaded ? null : "Asset load failed");

            var callbacks = new System.Collections.Generic.List<Action<bool>>(_assetLoaderCallbackList);
            _assetLoaderCallbackList.Clear();

            _dispatchDepth++;
            try
            {
                foreach (var loader in callbacks)
                {
                    LiteUtils.SafeInvoke(loader, isLoaded);
                }
            }
            finally
            {
                _dispatchDepth--;
            }
        }

        private Lease<T> TryAcquireLease<T>() where T : UnityEngine.Object
        {
            if (IsLoaded && Asset is T asset && asset != null)
            {
                return AcquireLease(asset);
            }

            if (Asset != null)
            {
                LLog.Error("Asset type mismatch : {0}, expected {1}, actual {2}", _assetPath, typeof(T).Name, Asset.GetType().Name);
            }
            return null;
        }
    }
}
