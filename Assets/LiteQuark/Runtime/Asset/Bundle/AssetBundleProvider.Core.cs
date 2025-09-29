using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        private IBundleLocater _bundleLocater = null;
        private VersionPackInfo _packInfo = null;
        private AssetBundleLoader _bundleLoader = null;
        
        private readonly Dictionary<string, AssetBundleCache> _bundleCacheMap = new();
        private readonly Dictionary<int, AssetIDToPathData> _assetIDToPathMap = new();
        private readonly List<string> _unloadBundleList = new();
        
        public AssetBundleProvider()
        {
        }
        
        public async UniTask<bool> Initialize()
        {
            _bundleLocater = CreateLocater();
            if (_bundleLocater == null)
            {
                return false;
            }
            
            _packInfo = await _bundleLocater.LoadVersionPack(AppUtils.GetVersionFileName(), null).Task as VersionPackInfo;
            if (_packInfo == null)
            {
                return false;
            }

            _bundleLoader = new AssetBundleLoader(_bundleLocater, LiteRuntime.Setting.Asset.ConcurrencyLimit);

            _bundleCacheMap.Clear();
            _assetIDToPathMap.Clear();
            _unloadBundleList.Clear();
            return true;
        }

        public void Dispose()
        {
            _unloadBundleList.Clear();
            _assetIDToPathMap.Clear();
            foreach (var chunk in _bundleCacheMap)
            {
                chunk.Value.Dispose();
            }
            _bundleCacheMap.Clear();
        }
        
        public void Tick(float deltaTime)
        {
            _bundleLoader.Tick(deltaTime);
            
            foreach (var chunk in _bundleCacheMap)
            {
                chunk.Value.Tick(deltaTime);

                if (chunk.Value.Stage == AssetCacheStage.Unloading)
                {
                    _unloadBundleList.Add(chunk.Key);
                }
            }

            if (_unloadBundleList.Count > 0)
            {
                foreach (var bundlePath in _unloadBundleList)
                {
                    UnloadBundleCache(bundlePath);
                }
                _unloadBundleList.Clear();
            }
        }
        
        public string GetVersion()
        {
            return _packInfo?.Version ?? AppUtils.GetVersion();
        }
        
        internal AssetBundleCache GetOrCreateBundleCache(string bundlePath)
        {
            if (_bundleCacheMap.TryGetValue(bundlePath, out var cache))
            {
                return cache;
            }
            
            var bundleInfo = _packInfo.GetBundleInfoFromBundlePath(bundlePath);
            if (bundleInfo == null)
            {
                return null;
            }

            cache = new AssetBundleCache(this, bundleInfo);
            _bundleCacheMap.Add(bundlePath, cache);
            return cache;
        }

        public void PreloadBundle(string bundlePath, Action<bool> callback)
        {
            var cache = GetOrCreateBundleCache(bundlePath);
            cache.LoadBundleAsync(0, callback);
        }
        
        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            LoadAssetAsync<T>(assetPath, (asset) =>
            {
                LiteUtils.SafeInvoke(callback, asset != null);
            });
        }
        
        public void UnloadAsset(string assetPath)
        {
            var bundleInfo = _packInfo.GetBundleInfoFromAssetPath(assetPath);
            if (bundleInfo == null)
            {
                return;
            }

            if (_bundleCacheMap.TryGetValue(bundleInfo.BundlePath, out var cache) && cache.IsLoaded)
            {
                cache.UnloadAsset(assetPath);
            }
        }
        
        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset == null)
            {
                return;
            }

            var instanceID = asset.GetInstanceID();
            if (_assetIDToPathMap.TryGetValue(instanceID, out var pathCache))
            {
                UnloadAsset(pathCache.AssetPath);
                pathCache.Count--;
                if (pathCache.Count <= 0)
                {
                    _assetIDToPathMap.Remove(instanceID);
                }
            }

            if (asset is UnityEngine.GameObject go)
            {
                if (go.scene.isLoaded)
                {
                    UnityEngine.Object.Destroy(asset);
                }
            }
        }

        public void UnloadSceneAsync(string scenePath, string sceneName, Action callback)
        {
            var bundleInfo = _packInfo.GetBundleInfoFromAssetPath(scenePath);
            if (bundleInfo == null)
            {
                return;
            }
            
            if (_bundleCacheMap.TryGetValue(bundleInfo.BundlePath, out var cache) && cache.IsLoaded)
            {
                cache.UnloadSceneAsync(sceneName, callback);
            }
        }

        public void UnloadUnusedAssets(int maxDepth)
        {
            var depth = maxDepth;
            while (depth > 0)
            {
                depth--;
                var unloadList = new List<string>();

                foreach (var chunk in _bundleCacheMap)
                {
                    chunk.Value.UnloadUnusedAssets();

                    if (chunk.Value.Stage == AssetCacheStage.Retained || chunk.Value.Stage == AssetCacheStage.Unloading)
                    {
                        unloadList.Add(chunk.Key);
                    }
                }

                if (unloadList.Count > 0)
                {
                    foreach (var bundlePath in unloadList)
                    {
                        UnloadBundleCache(bundlePath);
                    }

                    unloadList.Clear();
                }
                else
                {
                    break;
                }
            }

            UnityEngine.Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        
        private void UnloadBundleCache(string bundlePath)
        {
            if (_bundleCacheMap.ContainsKey(bundlePath))
            {
                // BundleCacheMap_[bundlePath].Unload(false);
                _bundleCacheMap[bundlePath].Dispose();
                _bundleCacheMap.Remove(bundlePath);
            }
        }
        
        private class AssetIDToPathData
        {
            internal string AssetPath { get; }
            internal int Count { get; set; }

            public AssetIDToPathData(string assetPath)
            {
                AssetPath = assetPath;
                Count = 1;
            }
        }

        private void UpdateAssetIDToPathMap(UnityEngine.Object asset, string assetPath)
        {
            if (asset != null)
            {
                var instanceID = asset.GetInstanceID();
                if (_assetIDToPathMap.TryGetValue(instanceID, out var data))
                {
                    data.Count++;
                }
                else
                {
                    _assetIDToPathMap.Add(instanceID, new AssetIDToPathData(assetPath));
                }
            }
        }
        
        internal void LoadBundle(BundleInfo bundleInfo, int priority, Action<UnityEngine.AssetBundle> callback)
        {
            var bundlePath = _packInfo.GetBundleFileLoadPath(bundleInfo);
            // return _bundleLocater.LoadBundle(bundlePath, callback);
            _bundleLoader.LoadBundle(bundlePath, priority, callback);
        }

        private IBundleLocater CreateLocater()
        {
            switch (LiteRuntime.Setting.Asset.BundleLocater)
            {
                case BundleLocaterMode.BuiltIn:
                    return new BundleBuiltInLocater
                    {
                        EditorForceStreamingAssets = LiteRuntime.Setting.Asset.EditorForceStreamingAssets
                    };
                case BundleLocaterMode.Remote:
                    return new BundleRemoteLocater(PathUtils.ConcatPath(
                        LiteRuntime.Setting.Asset.BundleRemoteUri,
                        AppUtils.GetCurrentPlatform(),
                        AppUtils.GetVersion()));
                default:
                    throw new ArgumentException($"error {nameof(BundleLocaterMode)} : {LiteRuntime.Setting.Asset.BundleLocater}");
            }
        }
    }
}