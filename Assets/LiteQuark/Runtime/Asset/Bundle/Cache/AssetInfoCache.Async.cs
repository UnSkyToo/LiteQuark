using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        public void LoadAssetAsync<T>(Lease<T> lease, Action<AssetLoadResult<IAssetLease<T>>> callback) where T : UnityEngine.Object
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                lease.Dispose();
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(new AssetError(_assetPath, "Asset cache is unloading or unloaded")));
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
                    var error = new AssetError(_assetPath, $"Asset is unavailable or does not match {typeof(T).Name}");
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(error));
                    return;
                }
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Success(lease));
                return;
            }

            _assetLoaderCallbackList.Add(result =>
            {
                var asset = result.Value;
                var success = result.Succeeded && lease.TryBindAsset(asset);
                if (!success)
                {
                    var error = result.Error ?? new AssetError(_assetPath, $"Asset is unavailable or does not match {typeof(T).Name}");
                    lease.Dispose();
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(error));
                    return;
                }
                LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Success(lease));
            });
            if (Stage != AssetCacheStage.Idle)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Asset, _assetPath, _assetType, _cache.BundlePath);

            try
            {
                _loadAssetTask = LiteRuntime.Task.AddLoadAssetTask<T>(_cache.Bundle, _assetPath, HandleAssetLoadCompleted);
            }
            catch (Exception ex)
            {
                CompleteAssetLoad(AssetLoadResult<UnityEngine.Object>.Failure(new AssetError(_assetPath, ex.Message, ex)));
                throw;
            }
        }

        private void HandleAssetLoadCompleted(ITask task)
        {
            var asset = task.Result as UnityEngine.Object;
            var result = task.State == TaskState.Succeeded && asset != null
                ? AssetLoadResult<UnityEngine.Object>.Success(asset)
                : AssetLoadResult<UnityEngine.Object>.Failure(new AssetError(_assetPath, task.Error?.Message ?? "Asset load returned no result", task.Error));
            CompleteAssetLoad(result);
        }

        private void CompleteAssetLoad(AssetLoadResult<UnityEngine.Object> result)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                return;
            }

            _loadAssetTask = null;

            var isLoaded = OnAssetLoaded(result.Value);

            var callbacks = new System.Collections.Generic.List<Action<AssetLoadResult<UnityEngine.Object>>>(_assetLoaderCallbackList);
            _assetLoaderCallbackList.Clear();

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Asset, _assetPath, _assetType, _cache.BundlePath, isLoaded, errorMessage: isLoaded ? null : "Asset load failed");

            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, result);
            }
        }
    }
}
