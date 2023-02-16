using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : IDisposable
    {
        public BundleInfo Info { get; }
        public bool IsLoaded { get; private set; }
        private bool IsUnloaded_;
        
        public bool IsUsed => RefCount_ > 0;
        private int RefCount_;
        
        private readonly List<AssetBundleCache> DependencyCacheList_ = new();
        private readonly Dictionary<string, UnityEngine.Object> AssetCacheMap_ = new();
        private readonly Dictionary<string, List<Action<bool>>> AssetLoaderCallbackList_ = new();
        private readonly Dictionary<string, UnityEngine.AssetBundleRequest> AssetRequestMap_ = new();
        private readonly Dictionary<UnityEngine.AsyncOperation, string> RequestToAssetPathMap_ = new();

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
            AssetLoaderCallbackList_.Clear();
            DependencyCacheList_.Clear();
            AssetCacheMap_.Clear();
            AssetRequestMap_.Clear();

            if (Bundle_ != null)
            {
                Bundle_.Unload(true);
                Bundle_ = null;
            }

            BundleRequest_ = null;
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

        public void IncRef()
        {
            RefCount_++;
        }

        public void DecRef()
        {
            RefCount_--;
        }

        private bool AssetExisted(string assetPath)
        {
            return AssetCacheMap_.ContainsKey(assetPath);
        }

        public UnityEngine.Object[] GetLoadAssetList()
        {
            return AssetCacheMap_.Count == 0 ? Array.Empty<UnityEngine.Object>() : AssetCacheMap_.Values.ToArray();
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
    }
}