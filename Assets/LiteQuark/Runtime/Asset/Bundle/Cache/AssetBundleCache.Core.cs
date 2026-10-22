using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.AssetBundle Bundle { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded;
        public bool IsRetained => Stage == AssetCacheStage.Loaded && _refCount == 0;
        internal string BundlePath => _bundleInfo?.BundlePath ?? string.Empty;
        internal string[] DependencyList => _bundleInfo?.DependencyList ?? Array.Empty<string>();
        internal long FileSize => _bundleInfo?.Size ?? 0;

        private readonly BundleInfo _bundleInfo;
        private readonly AssetBundleProvider _provider;
        private readonly List<Action<bool>> _bundleLoaderCallbackList = new();
        private readonly Dictionary<(string, Type), AssetInfoCache> _assetCacheMap = new();
        private readonly List<SceneInfoCache> _sceneCacheMap = new();

        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private float _retainTime;
        private AssetLoadPriority _loadPriority;

        public AssetBundleCache(AssetBundleProvider provider, BundleInfo bundleInfo)
        {
            Stage = AssetCacheStage.Idle;
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
                var callbacks = new List<Action<bool>>(_bundleLoaderCallbackList);
                _bundleLoaderCallbackList.Clear();
                foreach (var callback in callbacks)
                {
                    LiteUtils.SafeInvoke(callback, false);
                }
            }
        }

        internal bool CanUnload(bool ignoreRetainTime)
        {
            if (IsUsed)
            {
                return false;
            }

            return Stage switch
            {
                AssetCacheStage.Idle => true,
                AssetCacheStage.Loaded => ignoreRetainTime || _retainTime <= 0f,
                _ => false
            };
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
            if (IsRetained)
            {
                _retainTime -= deltaTime;
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
            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Idle && Stage != AssetCacheStage.Loading)
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
                 Stage != AssetCacheStage.Idle &&
                 Stage != AssetCacheStage.Loading))
            {
                LLog.Error("Bundle DecRef error, {0} : {1}", _bundleInfo.BundlePath, Stage);
                return;
            }

            _refCount--;

            if (_refCount == 0 && Stage == AssetCacheStage.Loaded)
            {
                _retainTime = LiteRuntime.Setting.Asset.EnableRetain
                    ? LiteRuntime.Setting.Asset.BundleRetainTime
                    : 0f;
            }
        }

        private AssetInfoCache GetOrCreateAssetCache(string assetKey, Type assetType)
        {
            var key = (assetKey, assetType);
            if (_assetCacheMap.TryGetValue(key, out var cache))
            {
                return cache;
            }

            cache = new AssetInfoCache(this, assetKey, assetType);
            _assetCacheMap.Add(key, cache);
            return cache;
        }

        private SceneInfoCache CreateSceneCache(string sceneKey, string loadPath, UnityEngine.SceneManagement.LoadSceneParameters parameters, Lease bundleLease)
        {
            var cache = new SceneInfoCache(bundleLease, sceneKey, loadPath, parameters);
            _sceneCacheMap.Add(cache);
            return cache;
        }

        private void RaiseLoadPriority(AssetLoadPriority priority)
        {
            if (Stage != AssetCacheStage.Loading || priority <= _loadPriority)
            {
                return;
            }

            _loadPriority = priority;
            _provider.RaiseBundlePriority(_bundleInfo, priority);

            foreach (var dependency in DependencyList)
            {
                if (_provider.TryGetBundleCache(dependency, out var dependencyCache))
                {
                    dependencyCache.RaiseLoadPriority(priority);
                }
            }
        }

        private bool TryGetSceneLoadPath(string sceneKey, out string loadPath)
        {
            loadPath = null;

            foreach (var candidate in Bundle.GetAllScenePaths())
            {
                var candidateKey = AssetPath.ResolveScene(candidate);

                if (!string.Equals(candidateKey, sceneKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (loadPath != null)
                {
                    LLog.Error("Duplicate scene key in bundle: {0}", sceneKey);
                    loadPath = null;
                    return false;
                }

                // 保留 Bundle 返回的实际大小写。
                loadPath = candidate;
            }

            return loadPath != null;
        }

        private bool OnBundleLoaded(UnityEngine.AssetBundle bundle)
        {
            Bundle = bundle;

            if (bundle == null)
            {
                Stage = AssetCacheStage.Idle;
                LLog.Error("Load bundle failed : {0}", _bundleInfo.BundlePath);
                return false;
            }

            Stage = AssetCacheStage.Loaded;
            return true;
        }
    }
}
