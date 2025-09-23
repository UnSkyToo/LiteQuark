using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        private VersionPackInfo _packInfo = null;
        
        private readonly Dictionary<string, AssetBundleCache> _bundleCacheMap = new();
        private readonly Dictionary<int, AssetIDToPathData> _assetIDToPathMap = new();
        private readonly List<string> _unloadBundleList = new();
        private bool _isEnableRemoteBundle = false;
        private string _bundleRemoteUri = string.Empty;
        
        public AssetBundleProvider()
        {
        }
        
        public async UniTask<bool> Initialize()
        {
            var versionPackUri = string.Empty;
            
            _isEnableRemoteBundle = LiteRuntime.Setting.Asset.EnableRemoteBundle;
            if (_isEnableRemoteBundle)
            {
                _bundleRemoteUri = PathUtils.ConcatPath(
                    LiteRuntime.Setting.Asset.BundleRemoteUri,
                    AppUtils.GetCurrentPlatform(),
                    AppUtils.GetVersion()).ToLower();
                LLog.Info("BundleRemoteUri: " + _bundleRemoteUri);

                versionPackUri = PathUtils.ConcatPath(_bundleRemoteUri, AppUtils.GetVersionFileName());
            }
            else
            {
                versionPackUri = PathUtils.GetFullPathInRuntime(AppUtils.GetVersionFileName());
                
#if UNITY_EDITOR
                if (LiteRuntime.Setting.Asset.EditorForceStreamingAssets)
                {
                    versionPackUri = PathUtils.GetStreamingAssetsPath(LiteConst.Tag, AppUtils.GetVersionFileName());
                }
#endif
            }

            _packInfo = await VersionPackInfo.LoadPackAsync(versionPackUri);
            if (_packInfo == null)
            {
                return false;
            }

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
            cache.LoadBundleAsync(callback);
        }
        
        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            LoadAssetAsync<T>(assetPath, (asset) =>
            {
                callback?.Invoke(asset != null);
            });
        }
        
        public void UnloadAsset(string assetPath)
        {
            var info = _packInfo.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            if (_bundleCacheMap.TryGetValue(info.BundlePath, out var cache) && cache.IsLoaded)
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

        public void UnloadSceneAsync(string scenePath, Action callback)
        {
            var info = _packInfo.GetBundleInfoFromAssetPath(scenePath);
            if (info == null)
            {
                return;
            }

            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            if (_bundleCacheMap.TryGetValue(info.BundlePath, out var cache) && cache.IsLoaded)
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

        internal string GetBundleUri(BundleInfo bundle)
        {
            var bundleName = _packInfo.GetBundlePath(bundle);
            if (_isEnableRemoteBundle)
            {
                return PathUtils.ConcatPath(_bundleRemoteUri, bundleName);
            }
            return PathUtils.GetFullPathInRuntime(bundleName);
        }

        internal bool IsEnableRemoteBundle()
        {
            return _isEnableRemoteBundle;
        }
    }
}