using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        public AssetBundleCache Cache { get; private set; }
        public UnityEngine.Object Asset { get; private set; }
        public bool IsLoaded { get; private set; }
        private bool IsUnloaded_;
        
        public bool IsUsed => RefCount_ > 0;
        private int RefCount_;

        private readonly string AssetPath_;
        private readonly List<Action<bool>> AssetLoaderCallbackList_ = new();
        private UnityEngine.AssetBundleRequest AssetRequest_ = null;
        
        public AssetInfoCache(AssetBundleCache cache, string assetPath)
        {
            Cache = cache;
            AssetPath_ = assetPath;
            IsLoaded = false;
            IsUnloaded_ = false;

            AssetRequest_ = null;
            Asset = null;
            RefCount_ = 0;
        }

        public void Dispose()
        {
            Unload();
            
            AssetLoaderCallbackList_.Clear();
            AssetRequest_ = null;
            Asset = null;
        }

        public void Unload()
        {
            if (IsUnloaded_)
            {
                return;
            }
            
            if (RefCount_ > 0)
            {
                LLog.Warning($"unload asset leak : {AssetPath_}({RefCount_})");
            }

            RefCount_ = 0;
            IsUnloaded_ = true;
        }

        private void IncRef()
        {
            RefCount_++;
        }

        private void DecRef()
        {
            RefCount_--;
        }
        
        private void OnAssetLoaded(UnityEngine.Object asset)
        {
            if (asset != null)
            {
                Asset = asset;
                AssetRequest_ = null;
                RefCount_ = 0;
                IsLoaded = true;
                IsUnloaded_ = false;
            }
        }

        public void UnloadAsset(string assetPath)
        {
            if (AssetPath_ != assetPath)
            {
                return;
            }
            
            DecRef();
        }
    }
}