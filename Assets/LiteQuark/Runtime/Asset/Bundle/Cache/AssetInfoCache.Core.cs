using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.Object Asset { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded;

        private readonly string _assetPath;
        private readonly AssetBundleCache _cache;
        private readonly List<Action<bool>> _assetLoaderCallbackList = new();
        private AssetBundleCache.Lease _mainLease;

        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private BaseTask _loadAssetTask;

        public AssetInfoCache(AssetBundleCache cache, string assetPath)
        {
            Stage = AssetCacheStage.Created;
            Asset = null;

            _assetPath = assetPath;
            _cache = cache;
            _refCount = 0;
        }

        public void Dispose()
        {
            Unload();

            _loadAssetTask?.Cancel();
            _loadAssetTask = null;
            Asset = null;

            var callbacks = new List<Action<bool>>(_assetLoaderCallbackList);
            _assetLoaderCallbackList.Clear();
            foreach (var cb in callbacks)
            {
                LiteUtils.SafeInvoke(cb, false);
            }
        }

        public void Unload()
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                return;
            }

            if (_refCount > 0)
            {
                LLog.Warning("Unload asset leak : {0}({1})", _assetPath, _refCount);
            }

            Stage = AssetCacheStage.Unloading;
            _refCount = 0;
            var lease = _mainLease;
            _mainLease = null;
            lease?.Dispose();
            Stage = AssetCacheStage.Unloaded;
        }

        private void IncRef()
        {
            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Created && Stage != AssetCacheStage.Loading)
            {
                LLog.Error("Asset IncRef error, {0} : {1}", _assetPath, Stage);
                throw new ObjectDisposedException(nameof(AssetInfoCache));
            }

            _mainLease ??= _cache.AcquireLease();
            _refCount++;
        }

        private void DecRef()
        {
            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Created && Stage != AssetCacheStage.Loading)
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
                Stage = AssetCacheStage.Created;
                LLog.Error("Load asset failed : {0}", _assetPath);
                return false;
            }

            Asset = asset;
            Stage = AssetCacheStage.Loaded;
            return true;
        }
    }
}
