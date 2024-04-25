using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : IDispose
    {
        public BundleInfo Info { get; }
        public UnityEngine.AssetBundle Bundle => Bundle_;
        public bool IsLoaded { get; private set; }
        private bool IsUnloaded_;
        
        public bool IsUsed => RefCount_ > 0;
        private int RefCount_;
        
        private readonly List<AssetBundleCache> DependencyCacheList_ = new();
        private readonly Dictionary<string, AssetInfoCache> AssetCacheMap_ = new();

        private UnityEngine.AssetBundleCreateRequest BundleRequest_;
        private UnityEngine.AssetBundle Bundle_;
        private Action<bool> BundleLoaderCallback_;

        public AssetBundleCache(BundleInfo info)
        {
            Info = info;
            IsLoaded = false;
            IsUnloaded_ = false;

            BundleRequest_ = null;
            Bundle_ = null;
            RefCount_ = 0;
        }

        public void Dispose()
        {
            Unload();
            DependencyCacheList_.Clear();
            AssetCacheMap_.Clear();

            if (Bundle_ != null)
            {
                Bundle_.Unload(true);
                Bundle_ = null;
            }

            BundleRequest_ = null;
            BundleLoaderCallback_ = null;
            IsLoaded = false;
        }
        
        public void Unload()
        {
            if (IsUnloaded_)
            {
                return;
            }
            
            if (RefCount_ > 0)
            {
                LLog.Warning($"unload bundle leak : {Info.BundlePath}({RefCount_})");
            }
            
            foreach (var cache in DependencyCacheList_)
            {
                cache.DecRef();

                if (!cache.IsUsed)
                {
                    cache.Unload();
                }
            }

            RefCount_ = 0;
            IsUnloaded_ = true;
        }

        public string[] GetAllDependencies()
        {
            return Info.DependencyList;
        }
        
        public void AddDependencyCache(AssetBundleCache cache)
        {
            cache.IncRef();
            DependencyCacheList_.Add(cache);
        }

        private void IncRef()
        {
            RefCount_++;
        }

        private void DecRef()
        {
            RefCount_--;
        }

        private AssetInfoCache GetOrCreateAssetCache(string assetPath)
        {
            if (AssetCacheMap_.TryGetValue(assetPath, out var cache))
            {
                return cache;
            }

            cache = new AssetInfoCache(this, assetPath);
            AssetCacheMap_.Add(assetPath, cache);
            return cache;
        }

        public AssetInfoCache[] GetLoadAssetList()
        {
            return AssetCacheMap_.Count == 0 ? Array.Empty<AssetInfoCache>() : AssetCacheMap_.Values.ToArray();
        }

        private void OnBundleLoaded(UnityEngine.AssetBundle bundle)
        {
            if (bundle != null)
            {
                Bundle_ = bundle;
                BundleRequest_ = null;
                RefCount_ = 0;
                IsLoaded = true;
                IsUnloaded_ = false;
            }
        }

        public void UnloadAsset(string assetPath)
        {
            if (AssetCacheMap_.TryGetValue(assetPath, out var cache))
            {
                cache.UnloadAsset(assetPath);
                DecRef();
            }
        }
    }
}