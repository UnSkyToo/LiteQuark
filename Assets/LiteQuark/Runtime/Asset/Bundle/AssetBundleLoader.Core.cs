using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleLoader : IAssetLoader
    {
        private BundlePackInfo PackInfo_ = null;
        
        private readonly Dictionary<string, AssetBundleCache> BundleCacheMap_ = new();
        private readonly Dictionary<int, string> AssetIDToPathMap_ = new();
        private readonly List<string> UnloadBundleList_ = new();
        
        public AssetBundleLoader()
        {
        }
        
        public bool Initialize()
        {
            PackInfo_ = BundlePackInfo.LoadBundlePack();
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

        internal BundlePackInfo GetPackInfo()
        {
            return PackInfo_;
        }
        
        internal AssetBundleCache GetOrCreateBundleCache(string bundlePath)
        {
            if (BundleCacheMap_.TryGetValue(bundlePath, out var cache))
            {
                return cache;
            }

            cache = new AssetBundleCache(this, bundlePath);
            BundleCacheMap_.Add(bundlePath, cache);
            return cache;
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

            if (BundleCacheMap_.TryGetValue(info.BundlePath, out var cache) && cache.Stage == AssetCacheStage.Loaded)
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
            if (AssetIDToPathMap_.TryGetValue(instanceID, out var assetPath))
            {
                UnloadAsset(assetPath);
                AssetIDToPathMap_.Remove(instanceID);
            }

            if (asset is UnityEngine.GameObject)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        public void UnloadUnusedAssets()
        {
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

            UnityEngine.Resources.UnloadUnusedAssets();
            GC.Collect();
        }
        
        private void UnloadBundleCache(string bundlePath)
        {
            if (BundleCacheMap_.ContainsKey(bundlePath))
            {
                BundleCacheMap_[bundlePath].Unload(false);
                BundleCacheMap_.Remove(bundlePath);
            }
        }
    }
}