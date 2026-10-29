using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDisposeAsync
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.Object Asset { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded;

        private readonly string _assetPath;
        private readonly Type _assetType;
        private readonly AssetBundleCache _cache;
        private readonly List<Action<AssetLoadResult<UnityEngine.Object>>> _assetLoaderCallbackList = new();
        private AssetBundleCache.Lease _mainLease;

        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private BaseTask _loadAssetTask;

        public AssetInfoCache(AssetBundleCache cache, string assetPath, Type assetType)
        {
            Stage = AssetCacheStage.Idle;
            Asset = null;

            _assetPath = assetPath;
            _assetType = assetType;
            _cache = cache;
            _refCount = 0;
        }

        public async UniTask DisposeAsync()
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }
            Stage = AssetCacheStage.Unloading;
            var task = _loadAssetTask;
            task?.Cancel();

            if (_assetLoaderCallbackList.Count > 0)
            {
                var callbacks = new List<Action<AssetLoadResult<UnityEngine.Object>>>(_assetLoaderCallbackList);
                _assetLoaderCallbackList.Clear();
                var result = AssetLoadResult<UnityEngine.Object>.Failure(
                    new AssetError(_assetPath, "Asset cache was disposed before delivery"));
                foreach (var callback in callbacks)
                {
                    LiteUtils.SafeInvoke(callback, result);
                }
            }

            if (task is IResourceTaskCleanup cleanup)
            {
                await cleanup.WaitForCleanupAsync();
            }
            CompleteUnload();
        }

        // 原生读取已结束后使用；父 Bundle 的零引用回收复用这一同步步骤。
        internal void CompleteUnload()
        {
            _loadAssetTask = null;
            Asset = null;
            _refCount = 0;

            // 原生读取结束后才归还 Bundle 引用，正常缓存回收也遵循这一顺序。
            var lease = _mainLease;
            _mainLease = null;
            lease?.Dispose();
            Stage = AssetCacheStage.Unloaded;
        }

        private void IncRef()
        {
            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Idle && Stage != AssetCacheStage.Loading)
            {
                LLog.Error("Asset IncRef error, {0} : {1}", _assetPath, Stage);
                throw new ObjectDisposedException(nameof(AssetInfoCache));
            }

            _mainLease ??= _cache.AcquireLease();
            _refCount++;
        }

        private void DecRef()
        {
            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Idle && Stage != AssetCacheStage.Loading)
            {
                LLog.Error("Asset DecRef error, {0} : {1}", _assetPath, Stage);
                throw new ObjectDisposedException(nameof(AssetInfoCache));
            }

            if (_refCount <= 0)
            {
                return;
            }

            _refCount--;
            if (_refCount == 0)
            {
                var lease = _mainLease;
                _mainLease = null;
                lease?.Dispose();
            }
        }

        private bool OnAssetLoaded(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                Asset = null;
                Stage = AssetCacheStage.Idle;
                LLog.Error("Load asset failed : {0}", _assetPath);
                return false;
            }

            Asset = asset;
            Stage = AssetCacheStage.Loaded;
            return true;
        }
    }
}
