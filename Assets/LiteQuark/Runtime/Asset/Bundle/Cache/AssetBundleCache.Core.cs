using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.AssetBundle Bundle { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded || Stage == AssetCacheStage.Retained;

        private readonly BundleInfo _bundleInfo;
        private readonly AssetBundleProvider _provider;
        private readonly List<Action<bool>> _bundleLoaderCallbackList = new ();
        private readonly Dictionary<string, AssetInfoCache> _assetCacheMap = new();
        private readonly List<string> _unloadAssetList = new();
        
        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private float _retainTime;
        private LoadBundleBaseTask _loadBundleTask;

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
            
            _loadBundleTask = null;
            _bundleLoaderCallbackList.Clear();
            _unloadAssetList.Clear();

            if (Bundle != null)
            {
                Bundle.Unload(true);
                Bundle = null;
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
            
            if (_refCount > 0 && !(Stage == AssetCacheStage.Retained || Stage == AssetCacheStage.Unloading))
            {
                LLog.Warning("Unload bundle leak : {0}({1})", _bundleInfo.BundlePath, _refCount);
            }
            
            foreach (var dependency in _bundleInfo.DependencyList)
            {
                var cache = _provider.GetOrCreateBundleCache(dependency);
                if (cache.Stage == AssetCacheStage.Unloaded)
                {
                    continue;
                }
                cache.DecRef();
            }

            _refCount = 0;
            Stage = AssetCacheStage.Unloaded;
        }
        
        public void Tick(float deltaTime)
        {
            if (Stage == AssetCacheStage.Retained)
            {
                _retainTime -= deltaTime;
                if (_retainTime <= 0f)
                {
                    Stage = AssetCacheStage.Unloading;
                }
            }
            
            foreach (var chunk in _assetCacheMap)
            {
                chunk.Value.Tick(deltaTime);

                if (chunk.Value.Stage == AssetCacheStage.Unloading)
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
            }
            
            _refCount--;

            if (_refCount <= 0)
            {
                if (LiteRuntime.Setting.Asset.EnableRetain)
                {
                    Stage = AssetCacheStage.Retained;
                    _retainTime = LiteRuntime.Setting.Asset.BundleRetainTime;
                }
                else
                {
                    Stage = AssetCacheStage.Unloading;
                    _retainTime = 0f;
                }
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
            
            if (bundle == null)
            {
                Stage = AssetCacheStage.Unloading;
                LLog.Error("Load bundle failed : {0}", _bundleInfo.BundlePath);
                return false;
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

        public void UnloadSceneAsync(string sceneName, Action callback)
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
                if (chunk.Value.Stage == AssetCacheStage.Retained || chunk.Value.Stage == AssetCacheStage.Unloading)
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