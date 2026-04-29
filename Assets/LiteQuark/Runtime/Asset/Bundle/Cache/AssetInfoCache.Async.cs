using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(Action<bool> callback) where T : UnityEngine.Object
        {
            if (IsLoaded)
            {
                var requestSucceeded = TryAcquireReference<T>();
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath, requestSucceeded,
                    errorMessage: requestSucceeded ? null : "Asset type mismatch", isCached: true);
                LiteUtils.SafeInvoke(callback, requestSucceeded);
                return;
            }

            _assetLoaderCallbackList.Add((isLoaded) =>
            {
                var requestSucceeded = isLoaded && TryAcquireReference<T>();
                LiteUtils.SafeInvoke(callback, requestSucceeded);
            });
            if (Stage != AssetCacheStage.Created)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(_assetPath);

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath);

            _loadAssetTask = LiteRuntime.Task.AddLoadAssetTask<T>(_cache.Bundle, name, HandleAssetLoadCompleted);
        }

        private void HandleAssetLoadCompleted(UnityEngine.Object asset)
        {
            _loadAssetTask = null;

            var isLoaded = OnAssetLoaded(asset);
            
            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath, isLoaded, errorMessage: isLoaded ? null : "Asset load failed");
            
            var callbacks = new System.Collections.Generic.List<Action<bool>>(_assetLoaderCallbackList);
            _assetLoaderCallbackList.Clear();
            
            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }
        }

        private bool TryAcquireReference<T>() where T : UnityEngine.Object
        {
            if (Asset is T)
            {
                IncRef();
                return true;
            }

            if (Asset != null)
            {
                LLog.Error("Asset type mismatch : {0}, expected {1}, actual {2}", _assetPath, typeof(T).Name, Asset.GetType().Name);
            }
            return false;
        }
    }
}
