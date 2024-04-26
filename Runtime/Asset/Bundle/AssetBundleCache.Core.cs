using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public BundleInfo Info { get; }
        
        private readonly List<AssetBundleCache> DependencyCacheList_ = new();
        private readonly Dictionary<string, AssetInfoCache> AssetCacheMap_ = new();
        private readonly List<string> UnloadAssetList_ = new();

        private UnityEngine.AssetBundleCreateRequest BundleRequest_;
        private UnityEngine.AssetBundle Bundle_;
        private Action<bool> BundleLoaderCallback_;
        
        public bool IsUsed => RefCount_ > 0;
        private int RefCount_;
        private float RetainTime_;

        public AssetBundleCache(BundleInfo info)
        {
            Stage = AssetCacheStage.Created;
            Info = info;

            BundleRequest_ = null;
            Bundle_ = null;
            BundleLoaderCallback_ = null;
            RefCount_ = 0;
            RetainTime_ = 0;
        }

        public void Dispose()
        {
            Unload();
            DependencyCacheList_.Clear();
            UnloadAssetList_.Clear();

            if (Bundle_ != null)
            {
                Bundle_.Unload(true);
                Bundle_ = null;
            }

            BundleRequest_ = null;
            BundleLoaderCallback_ = null;
        }
        
        public void Unload()
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }

            foreach (var chunk in AssetCacheMap_)
            {
                chunk.Value.Dispose();
            }
            AssetCacheMap_.Clear();
            UnloadAssetList_.Clear();
            
            if (RefCount_ > 0)
            {
                LLog.Warning($"unload bundle leak : {Info.BundlePath}({RefCount_})");
            }
            
            foreach (var cache in DependencyCacheList_)
            {
                cache.DecRef();
            }

            RefCount_ = 0;
            Stage = AssetCacheStage.Unloaded;
        }
        
        public void Tick(float deltaTime)
        {
            if (Stage == AssetCacheStage.Retained)
            {
                RetainTime_ -= deltaTime;
                if (RetainTime_ <= 0f)
                {
                    Stage = AssetCacheStage.Unloading;
                }
            }
            
            foreach (var chunk in AssetCacheMap_)
            {
                chunk.Value.Tick(deltaTime);

                if (chunk.Value.Stage == AssetCacheStage.Unloading)
                {
                    UnloadAssetList_.Add(chunk.Key);
                }
            }

            if (UnloadAssetList_.Count > 0)
            {
                foreach (var assetPath in UnloadAssetList_)
                {
                    AssetCacheMap_[assetPath].Unload();
                    AssetCacheMap_.Remove(assetPath);
                    DecRef();
                }
                UnloadAssetList_.Clear();
            }
        }
        
        public UnityEngine.AssetBundle GetBundle()
        {
            return Bundle_;
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
            if (Stage == AssetCacheStage.Retained)
            {
                Stage = AssetCacheStage.Loaded;
            }

            if (Stage != AssetCacheStage.Loaded)
            {
                LLog.Error($"bundle IncRef error, {Info.BundlePath} : {Stage}");
            }
            
            RefCount_++;
        }

        private void DecRef()
        {
            if (Stage != AssetCacheStage.Loaded)
            {
                LLog.Error($"bundle DecRef error, {Info.BundlePath} : {Stage}");
            }
            
            RefCount_--;

            if (RefCount_ <= 0)
            {
                Stage = AssetCacheStage.Retained;
                RetainTime_ = LiteRuntime.Setting.Asset.AssetRetainTime;
            }
        }

        private AssetInfoCache GetOrCreateAssetCache(string assetPath)
        {
            if (AssetCacheMap_.TryGetValue(assetPath, out var cache))
            {
                return cache;
            }

            cache = new AssetInfoCache(this, assetPath);
            AssetCacheMap_.Add(assetPath, cache);
            IncRef();
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
                Stage = AssetCacheStage.Loaded;
            }
            else
            {
                Stage = AssetCacheStage.Invalid;
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