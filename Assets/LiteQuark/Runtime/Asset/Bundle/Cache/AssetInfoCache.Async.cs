using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(Action<T> callback) where T : UnityEngine.Object
        {
            if (IsLoaded)
            {
                var asset = TryAcquireReference<T>();
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath, asset,
                    errorMessage: asset ? null : "Asset type mismatch", isCached: true);
                LiteUtils.SafeInvoke(callback, asset);
                return;
            }

            _assetLoaderCallbackList.Add(isLoaded =>
            {
                var asset = isLoaded ? TryAcquireReference<T>() : null;
                LiteUtils.SafeInvoke(callback, asset);
            });
            if (Stage != AssetCacheStage.Created)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(_assetPath);

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath);

            _loadAssetTask = LiteRuntime.Task.AddLoadAssetTask<T>(_bundleLease.Bundle, name, HandleAssetLoadCompleted);
        }

        private void HandleAssetLoadCompleted(UnityEngine.Object asset)
        {
            _loadAssetTask = null;

            var isLoaded = OnAssetLoaded(asset);

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _bundleLease.BundlePath, isLoaded, errorMessage: isLoaded ? null : "Asset load failed");

            var callbacks = new System.Collections.Generic.List<Action<bool>>(_assetLoaderCallbackList);
            _assetLoaderCallbackList.Clear();

            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }
        }

        private T TryAcquireReference<T>() where T : UnityEngine.Object
        {
            if (Asset is T asset && asset != null)
            {
                IncRef();
                return asset;
            }

            if (Asset != null)
            {
                LLog.Error("Asset type mismatch : {0}, expected {1}, actual {2}", _assetPath, typeof(T).Name, Asset.GetType().Name);
            }
            return null;
        }
    }
}
