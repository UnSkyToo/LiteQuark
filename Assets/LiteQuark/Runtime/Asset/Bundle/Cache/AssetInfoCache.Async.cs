using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(Action<bool> callback) where T : UnityEngine.Object
        {
            if (IsLoaded)
            {
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath, true, isCached: true);

                IncRef();
                LiteUtils.SafeInvoke(callback, true);
                return;
            }
            
            IncRef();
            _assetLoaderCallbackList.Add(callback);
            if (Stage != AssetCacheStage.Created)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(_assetPath);

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath);

            _loadAssetTask = LiteRuntime.Task.LoadAssetTask<T>(_cache.Bundle, name, HandleAssetLoadCompleted);
        }

        private void HandleAssetLoadCompleted(UnityEngine.Object asset)
        {
            _loadAssetTask = null;

            var isLoaded = OnAssetLoaded(asset);
            
            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _cache.BundlePath, isLoaded, errorMessage: isLoaded ? null : "Asset load failed");
            
            foreach (var loader in _assetLoaderCallbackList)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }
            
            _assetLoaderCallbackList.Clear();
        }
    }
}