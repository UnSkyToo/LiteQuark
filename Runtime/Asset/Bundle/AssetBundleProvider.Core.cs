using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        private BundlePackInfo PackInfo_ = null;
        
        private readonly Dictionary<string, AssetBundleCache> BundleCacheMap_ = new();
        private readonly Dictionary<int, AssetIDToPathData> AssetIDToPathMap_ = new();
        private readonly List<string> UnloadBundleList_ = new();
        private bool IsEnableRemoteBundle_ = false;
        private string BundleRemoteUri_ = string.Empty;
        
        public AssetBundleProvider()
        {
        }
        
        public async Task<bool> Initialize()
        {
            var bundlePackUri = string.Empty;
            
            IsEnableRemoteBundle_ = LiteRuntime.Setting.Asset.EnableRemoteBundle;
            if (IsEnableRemoteBundle_)
            {
                BundleRemoteUri_ = PathUtils.ConcatPath(
                    LiteRuntime.Setting.Asset.BundleRemoteUri,
                    AppUtils.GetCurrentPlatform(),
                    AppUtils.GetVersion()).ToLower();
                LLog.Info("BundleRemoteUri: " + BundleRemoteUri_);

                bundlePackUri = PathUtils.ConcatPath(BundleRemoteUri_, LiteConst.BundlePackFileName);
            }
            else
            {
                bundlePackUri = PathUtils.GetFullPathInRuntime(LiteConst.BundlePackFileName);
            }

            PackInfo_ = await BundlePackInfo.LoadBundlePackAsync(bundlePackUri);
            if (PackInfo_ == null)
            {
                return false;
            }

            BundleCacheMap_.Clear();
            AssetIDToPathMap_.Clear();
            UnloadBundleList_.Clear();
            return true;
        }

        public void Dispose()
        {
            UnloadBundleList_.Clear();
            AssetIDToPathMap_.Clear();
            foreach (var chunk in BundleCacheMap_)
            {
                chunk.Value.Dispose();
            }
            BundleCacheMap_.Clear();
        }
        
        public void Tick(float deltaTime)
        {
            foreach (var chunk in BundleCacheMap_)
            {
                chunk.Value.Tick(deltaTime);

                if (chunk.Value.Stage == AssetCacheStage.Unloading)
                {
                    UnloadBundleList_.Add(chunk.Key);
                }
            }

            if (UnloadBundleList_.Count > 0)
            {
                foreach (var bundlePath in UnloadBundleList_)
                {
                    UnloadBundleCache(bundlePath);
                }
                UnloadBundleList_.Clear();
            }
        }
        
        internal AssetBundleCache GetOrCreateBundleCache(string bundlePath)
        {
            if (BundleCacheMap_.TryGetValue(bundlePath, out var cache))
            {
                return cache;
            }
            
            var bundleInfo = PackInfo_.GetBundleInfoFromBundlePath(bundlePath);
            if (bundleInfo == null)
            {
                return null;
            }

            cache = new AssetBundleCache(this, bundleInfo);
            BundleCacheMap_.Add(bundlePath, cache);
            return cache;
        }

        public void PreloadBundle(string bundlePath, Action<bool> callback)
        {
            var cache = GetOrCreateBundleCache(bundlePath);
            cache.LoadBundleCompleteAsync(callback);
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
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            if (BundleCacheMap_.TryGetValue(info.BundlePath, out var cache) && cache.IsLoaded)
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
            if (AssetIDToPathMap_.TryGetValue(instanceID, out var pathCache))
            {
                UnloadAsset(pathCache.AssetPath);
                pathCache.Count--;
                if (pathCache.Count <= 0)
                {
                    AssetIDToPathMap_.Remove(instanceID);
                }
            }

            if (asset is UnityEngine.GameObject go)
            {
                if (go.scene.isLoaded)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }
        }

        public void UnloadSceneAsync(string scenePath, Action callback)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(scenePath);
            if (info == null)
            {
                return;
            }

            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            if (BundleCacheMap_.TryGetValue(info.BundlePath, out var cache) && cache.IsLoaded)
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

                foreach (var chunk in BundleCacheMap_)
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
            if (BundleCacheMap_.ContainsKey(bundlePath))
            {
                // BundleCacheMap_[bundlePath].Unload(false);
                BundleCacheMap_[bundlePath].Dispose();
                BundleCacheMap_.Remove(bundlePath);
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
                if (AssetIDToPathMap_.TryGetValue(instanceID, out var data))
                {
                    data.Count++;
                }
                else
                {
                    AssetIDToPathMap_.Add(instanceID, new AssetIDToPathData(assetPath));
                }
            }
        }

        internal string GetBundleUri(BundleInfo info)
        {
            var bundleName = PackInfo_.HashMode ? info.GetBundlePathWithHash() : info.BundlePath;
            if (IsEnableRemoteBundle_)
            {
                return PathUtils.ConcatPath(BundleRemoteUri_, bundleName);
            }
            return PathUtils.GetFullPathInRuntime(bundleName);
        }

        internal bool IsEnableRemoteBundle()
        {
            return IsEnableRemoteBundle_;
        }
    }
}