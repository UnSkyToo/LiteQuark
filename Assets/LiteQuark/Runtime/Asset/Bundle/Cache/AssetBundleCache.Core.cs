using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick
    {
        public AssetCacheStage Stage { get; private set; }
        // 主包交付后即持有；只有 Stage 为 Loaded 时，本包及依赖才全部就绪。
        public IBundleResource Bundle { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded;
        public bool IsRetained => Stage == AssetCacheStage.Loaded && _refCount == 0;
        internal string BundlePath => _bundleInfo?.BundlePath ?? string.Empty;
        internal string[] DependencyList => _bundleInfo?.DependencyList ?? Array.Empty<string>();
        internal long FileSize => _bundleInfo?.Size ?? 0;

        private readonly BundleInfo _bundleInfo;
        private readonly AssetManager _manager;
        private readonly List<Action<AssetLoadResult<IBundleResource>>> _bundleLoaderCallbackList = new();
        private readonly Dictionary<(string, Type), AssetInfoCache> _assetCacheMap = new();
        private Dictionary<string, SceneInfoCache> _sceneCacheMap;

        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private float _retainTime;
        private AssetLoadPriority _loadPriority;

        public AssetBundleCache(AssetManager manager, BundleInfo bundleInfo)
        {
            Stage = AssetCacheStage.Idle;
            Bundle = null;

            _bundleInfo = bundleInfo;
            _manager = manager;
            _refCount = 0;
            _retainTime = 0;
        }

        internal BundleVisitorInfo GetVisitorInfo()
        {
            var memSize = IsLoaded ? Bundle.GetRuntimeMemorySize() : 0L;
            return new BundleVisitorInfo(_bundleInfo.BundlePath,
                memSize,
                _refCount,
                Stage.ToString(),
                _retainTime,
                _sceneCacheMap?.Values.Where(scene => scene.Stage is not AssetCacheStage.Idle and not AssetCacheStage.Unloaded)
                    .Select(scene => scene.GetVisitorInfo()).ToArray() ?? Array.Empty<SceneVisitorInfo>(),
                _assetCacheMap.Values.Select(asset => asset.GetVisitorInfo()).ToArray());
        }

        internal void BeginReleaseContents(List<UniTask> cleanups)
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }
            Stage = AssetCacheStage.Unloading;

            foreach (var asset in _assetCacheMap.Values)
            {
                cleanups.Add(asset.DisposeAsync());
            }

            var scenes = _sceneCacheMap;
            _sceneCacheMap = null;
            if (scenes != null)
            {
                foreach (var scene in scenes.Values)
                {
                    cleanups.Add(scene.DisposeAsync());
                }
            }

            if (_bundleLoaderCallbackList.Count > 0)
            {
                var callbacks = new List<Action<AssetLoadResult<IBundleResource>>>(_bundleLoaderCallbackList);
                _bundleLoaderCallbackList.Clear();
                var result = AssetLoadResult<IBundleResource>.Failure(
                    new AssetError(BundlePath, "Bundle cache was disposed before delivery"));
                foreach (var callback in callbacks)
                {
                    LiteUtils.SafeInvoke(callback, result);
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

        internal bool TryUnloadUnused(bool ignoreRetainTime)
        {
            if (!CanUnload(ignoreRetainTime))
            {
                return false;
            }

            // 在途读取和场景卸载始终持有 Bundle Lease，零引用缓存无需取消或等待。
            Stage = AssetCacheStage.Unloading;
            foreach (var asset in _assetCacheMap.Values)
            {
                asset.CompleteUnload();
            }
            CompleteUnload();
            return true;
        }

        internal void CompleteUnload()
        {
            Stage = AssetCacheStage.Unloading;

            _assetCacheMap.Clear();
            _sceneCacheMap = null;

            if (_refCount > 0)
            {
                LLog.Warning("Unload bundle leak : {0}({1})", _bundleInfo.BundlePath, _refCount);
            }

            ReleaseBundle();
            ReleaseDependencyLeases();

            _refCount = 0;
            Stage = AssetCacheStage.Unloaded;
        }

        private void ReleaseBundle()
        {
            if (Bundle != null)
            {
                Bundle.Dispose();
                Bundle = null;
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsRetained)
            {
                _retainTime -= deltaTime;
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

        private bool TryCreateSceneCache(string sceneKey, out SceneInfoCache cache)
        {
            _sceneCacheMap ??= new(StringComparer.OrdinalIgnoreCase);
            if (_sceneCacheMap.TryGetValue(sceneKey, out var existing) && existing.Stage != AssetCacheStage.Unloaded)
            {
                cache = null;
                return false;
            }

            cache = new SceneInfoCache(AcquireLease(), sceneKey);
            _sceneCacheMap[sceneKey] = cache;
            return true;
        }

        private void RaiseLoadPriority(AssetLoadPriority priority)
        {
            if (Stage != AssetCacheStage.Loading || priority <= _loadPriority)
            {
                return;
            }

            _loadPriority = priority;
            _manager.RaiseBundlePriority(_bundleInfo, priority);

            foreach (var dependency in DependencyList)
            {
                if (_manager.TryGetBundleCache(dependency, out var dependencyCache))
                {
                    dependencyCache.RaiseLoadPriority(priority);
                }
            }
        }

        private bool TryGetSceneLoadPath(string sceneKey, out string loadPath, out AssetError error)
        {
            loadPath = null;
            error = null;

            if (!IsLoaded)
            {
                error = new AssetError(sceneKey, "Bundle cache closed before scene delivery");
                return false;
            }

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
                    error = new AssetError(sceneKey, "Duplicate scene key in bundle");
                    return false;
                }

                // 保留 Bundle 返回的实际大小写。
                loadPath = candidate;
            }

            if (loadPath == null)
            {
                error = new AssetError(sceneKey, "Scene path not found in bundle");
            }
            return loadPath != null;
        }

        private bool OnBundleLoaded()
        {
            if (Bundle == null)
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
