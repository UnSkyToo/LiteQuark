using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.AssetBundle Bundle { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded || Stage == AssetCacheStage.Retained;

        /// <summary>
        /// 资源是否过期了
        /// </summary>
        internal bool IsExpired => !IsDispatching &&
            ((Stage == AssetCacheStage.Retained && _retainTime <= 0) ||
            (Stage == AssetCacheStage.Created && !IsUsed && _bundleLoaderCallbackList.Count == 0));

        /// <summary>
        /// 资源是否为孤立资源（被加载了，但没有任何引用）
        /// </summary>
        internal bool IsOrphan => !IsDispatching &&
            ((Stage == AssetCacheStage.Loaded && !IsUsed && _bundleLoaderCallbackList.Count == 0) ||
            (Stage == AssetCacheStage.Created && !IsUsed && _bundleLoaderCallbackList.Count == 0));

        internal bool IsDispatching => _dispatchDepth > 0;

        internal string BundlePath => _bundleInfo?.BundlePath ?? string.Empty;
        internal string[] DependencyList => _bundleInfo?.DependencyList ?? Array.Empty<string>();
        internal long FileSize => _bundleInfo?.Size ?? 0;

        private readonly BundleInfo _bundleInfo;
        private readonly AssetBundleProvider _provider;
        private readonly List<Action<bool>> _bundleLoaderCallbackList = new();
        private readonly Dictionary<string, AssetInfoCache> _assetCacheMap = new();
        private readonly List<string> _unloadAssetList = new();
        private readonly List<SceneInfoCache> _sceneCacheMap = new();

        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private float _retainTime;
        private int _dispatchDepth;

        public AssetBundleCache(AssetBundleProvider provider, BundleInfo bundleInfo)
        {
            Stage = AssetCacheStage.Created;
            Bundle = null;

            _bundleInfo = bundleInfo;
            _provider = provider;
            _refCount = 0;
            _retainTime = 0;
        }

        public void Dispose()
        {
            try
            {
                Unload();
            }
            finally
            {
                _unloadAssetList.Clear();

                var callbacks = new List<Action<bool>>(_bundleLoaderCallbackList);
                _bundleLoaderCallbackList.Clear();
                foreach (var callback in callbacks)
                {
                    LiteUtils.SafeInvoke(callback, false);
                }
            }
        }

        public void Unload()
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                return;
            }

            Stage = AssetCacheStage.Unloading;

            foreach (var chunk in _assetCacheMap)
            {
                chunk.Value.Dispose();
            }

            _assetCacheMap.Clear();
            _unloadAssetList.Clear();

            foreach (var chunk in _sceneCacheMap)
            {
                chunk.Dispose();
            }

            _sceneCacheMap.Clear();

            if (_refCount > 0)
            {
                LLog.Warning("Unload bundle leak : {0}({1})", _bundleInfo.BundlePath, _refCount);
            }

            if (Bundle != null)
            {
                Bundle.Unload(true);
                Bundle = null;
            }

            ReleaseDependencyLeases();

            _refCount = 0;
            Stage = AssetCacheStage.Unloaded;
        }

        public void Tick(float deltaTime)
        {
            if (Stage == AssetCacheStage.Retained)
            {
                _retainTime -= deltaTime;
            }

            foreach (var chunk in _assetCacheMap)
            {
                chunk.Value.Tick(deltaTime);
                if (chunk.Value.IsExpired)
                {
                    _unloadAssetList.Add(chunk.Key);
                }
            }

            if (_unloadAssetList.Count > 0)
            {
                foreach (var assetPath in _unloadAssetList)
                {
                    UnloadAssetCache(assetPath);
                }

                _unloadAssetList.Clear();
            }

            for (var index = _sceneCacheMap.Count - 1; index >= 0; index--)
            {
                if (_sceneCacheMap[index].Stage == AssetCacheStage.Unloaded)
                {
                    _sceneCacheMap.RemoveAt(index);
                }
            }
        }

        private void IncRef()
        {
            if (Stage == AssetCacheStage.Retained)
            {
                Stage = AssetCacheStage.Loaded;
            }

            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Created && Stage != AssetCacheStage.Loading)
            {
                LLog.Error("Bundle IncRef error, {0} : {1}", _bundleInfo.BundlePath, Stage);
                throw new ObjectDisposedException(nameof(AssetBundleCache));
            }

            _refCount++;
        }

        private void DecRef()
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                if (_refCount > 0)
                {
                    _refCount--;
                }
                return;
            }

            if (_refCount <= 0 ||
                (Stage != AssetCacheStage.Loaded &&
                 Stage != AssetCacheStage.Created &&
                 Stage != AssetCacheStage.Loading))
            {
                LLog.Error("Bundle DecRef error, {0} : {1}", _bundleInfo.BundlePath, Stage);
                return;
            }

            _refCount--;

            if (_refCount == 0 && Stage == AssetCacheStage.Loaded)
            {
                Stage = AssetCacheStage.Retained;
                _retainTime = LiteRuntime.Setting.Asset.EnableRetain
                    ? LiteRuntime.Setting.Asset.BundleRetainTime
                    : 0f;
            }
        }

        private AssetInfoCache GetOrCreateAssetCache(string assetPath)
        {
            if (_assetCacheMap.TryGetValue(assetPath, out var cache))
            {
                return cache;
            }

            cache = new AssetInfoCache(this, assetPath);
            _assetCacheMap.Add(assetPath, cache);
            return cache;
        }

        private SceneInfoCache CreateSceneCache(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters)
        {
            var cache = new SceneInfoCache(AcquireLease(), sceneName, parameters);
            _sceneCacheMap.Add(cache);
            return cache;
        }

        private bool OnBundleLoaded(UnityEngine.AssetBundle bundle)
        {
            Bundle = bundle;

            if (bundle == null)
            {
                Stage = AssetCacheStage.Created;
                LLog.Error("Load bundle failed : {0}", _bundleInfo.BundlePath);
                return false;
            }

            Stage = AssetCacheStage.Loaded;
            return true;
        }

        public void UnloadUnusedAssets()
        {
            foreach (var chunk in _assetCacheMap)
            {
                if (!chunk.Value.IsDispatching && (chunk.Value.Stage == AssetCacheStage.Retained || chunk.Value.IsExpired))
                {
                    _unloadAssetList.Add(chunk.Key);
                }
            }

            if (_unloadAssetList.Count > 0)
            {
                foreach (var assetPath in _unloadAssetList)
                {
                    UnloadAssetCache(assetPath);
                }

                _unloadAssetList.Clear();
            }
        }

        private void UnloadAssetCache(string assetPath)
        {
            if (_assetCacheMap.TryGetValue(assetPath, out var cache))
            {
                cache.Unload();
                _assetCacheMap.Remove(assetPath);
            }
        }
    }
}
