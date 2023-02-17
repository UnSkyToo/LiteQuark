﻿using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleLoader : IAssetLoader
    {
        private BundlePackInfo PackInfo_ = null;
        
        private readonly Dictionary<string, AssetBundleCache> BundleCacheMap_ = new();
        private readonly Dictionary<string, List<Action<bool>>> BundleLoaderCallbackList_ = new();
        private readonly Dictionary<int, string> AssetIDToPathMap_ = new();
        
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
            BundleLoaderCallbackList_.Clear();
            AssetIDToPathMap_.Clear();
            
            return true;
        }

        public void Dispose()
        {
            AssetIDToPathMap_.Clear();
            BundleLoaderCallbackList_.Clear();
            foreach (var bundle in BundleCacheMap_)
            {
                bundle.Value.Dispose();
            }
            BundleCacheMap_.Clear();
        }

        private bool BundleExisted(BundleInfo info)
        {
            return BundleCacheMap_.TryGetValue(info.BundlePath, out var cache) && cache.IsLoaded;
        }
        
        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            LoadAssetAsync<T>(assetPath, (asset) =>
            {
                callback?.Invoke(asset != null);
            });
        }

        public void StopLoadAsset(string assetPath)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            BundleLoaderCallbackList_.Remove(info.BundlePath);
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
                cache.DecRef();
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

        public void UnloadUnusedBundle()
        {
            foreach (var bundle in BundleCacheMap_)
            {
                if (bundle.Value.IsLoaded && !bundle.Value.IsUsed)
                {
                    bundle.Value.Unload();
                }
            }

            var disposeList = new List<AssetBundleCache>();
            foreach (var bundle in BundleCacheMap_)
            {
                if (bundle.Value.IsLoaded && !bundle.Value.IsUsed)
                {
                    disposeList.Add(bundle.Value);
                }
            }

            if (disposeList.Count > 0)
            {
                foreach (var cache in disposeList)
                {
                    foreach (var asset in cache.GetLoadAssetList())
                    {
                        AssetIDToPathMap_.Remove(asset.GetInstanceID());
                    }

                    BundleCacheMap_.Remove(cache.Info.BundlePath);
                    cache.Dispose();
                }
                disposeList.Clear();
            }
            
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}