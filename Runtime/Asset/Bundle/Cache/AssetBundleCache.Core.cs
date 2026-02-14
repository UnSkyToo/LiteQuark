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
        internal bool IsExpired => Stage == AssetCacheStage.Retained && _retainTime <= 0;
        /// <summary>
        /// 资源是否为孤立资源（被加载了，但没有任何引用）
        /// </summary>
        internal bool IsOrphan => Stage == AssetCacheStage.Loaded && !IsUsed && _bundleLoaderCallbackList.Count == 0;
        
        internal string BundlePath => _bundleInfo?.BundlePath ?? string.Empty;
        internal string[] DependencyList => _bundleInfo?.DependencyList ?? Array.Empty<string>();
        internal long FileSize => _bundleInfo?.Size ?? 0;

        private readonly BundleInfo _bundleInfo;
        private readonly AssetBundleProvider _provider;
        private readonly List<Action<bool>> _bundleLoaderCallbackList = new ();
        private readonly Dictionary<string, AssetInfoCache> _assetCacheMap = new();
        private readonly List<string> _unloadAssetList = new();
        
        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private float _retainTime;

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
            Unload();
            _unloadAssetList.Clear();
            
            if (Bundle != null)
            {
                Bundle.Unload(true);
                Bundle = null;
            }
            
            var callbacks = new List<Action<bool>>(_bundleLoaderCallbackList);
            _bundleLoaderCallbackList.Clear();
            foreach (var callback in callbacks)
            {
                LiteUtils.SafeInvoke(callback, false);
            }
        }
        
        public void Unload()
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }

            foreach (var chunk in _assetCacheMap)
            {
                chunk.Value.Dispose();
            }
            _assetCacheMap.Clear();
            _unloadAssetList.Clear();
            
            if (_refCount > 0 && Stage != AssetCacheStage.Retained)
            {
                LLog.Warning("Unload bundle leak : {0}({1})", _bundleInfo.BundlePath, _refCount);
            }

            if (Bundle != null)
            {
                foreach (var dependency in _bundleInfo.DependencyList)
                {
                    if (_provider.TryGetBundleCache(dependency, out var cache))
                    {
                        if (cache.Stage == AssetCacheStage.Unloaded)
                        {
                            continue;
                        }

                        cache.DecRef();
                    }
                }
            }

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
            }
            
            _refCount++;
        }

        private void DecRef()
        {
            if (Stage != AssetCacheStage.Loaded)
            {
                LLog.Error("Bundle DecRef error, {0} : {1}", _bundleInfo.BundlePath, Stage);
                return;
            }
            
            _refCount--;

            if (_refCount <= 0)
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
            IncRef();
            return cache;
        }

        private bool OnBundleLoaded(UnityEngine.AssetBundle bundle)
        {
            if (bundle == null)
            {
                Stage = AssetCacheStage.Created;
                LLog.Error("Load bundle failed : {0}", _bundleInfo.BundlePath);
                return false;
            }
            
            Bundle = bundle;
            foreach (var dependency in _bundleInfo.DependencyList)
            {
                var cache = _provider.GetOrCreateBundleCache(dependency);
                if (cache.Stage == AssetCacheStage.Unloaded)
                {
                    continue;
                }
                cache.IncRef();
            }
            
            Stage = AssetCacheStage.Loaded;
            return true;
        }

        public void UnloadAsset(string assetPath)
        {
            if (_assetCacheMap.TryGetValue(assetPath, out var cache))
            {
                cache.UnloadAsset(assetPath);
                DecRef();
            }
        }

        public void UnloadSceneAsync(string scenePath, string sceneName, Action callback)
        {
            DecRef();
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            LiteRuntime.Task.AddTask(op, callback);
        }
        
        public void UnloadUnusedAssets()
        {
            var unloadList = new List<string>();
            
            foreach (var chunk in _assetCacheMap)
            {
                if (chunk.Value.IsExpired)
                {
                    unloadList.Add(chunk.Key);
                }
            }

            if (unloadList.Count > 0)
            {
                foreach (var assetPath in unloadList)
                {
                    UnloadAssetCache(assetPath);
                }
                unloadList.Clear();
            }
        }
        
        private void UnloadAssetCache(string assetPath)
        {
            if (_assetCacheMap.ContainsKey(assetPath))
            {
                _assetCacheMap[assetPath].Unload();
                _assetCacheMap.Remove(assetPath);
                DecRef();
            }
        }
    }
}