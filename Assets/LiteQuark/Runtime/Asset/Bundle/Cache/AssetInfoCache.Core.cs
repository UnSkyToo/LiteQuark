﻿using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.Object Asset { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded || Stage == AssetCacheStage.Retained;

        private readonly string AssetPath_;
        private readonly AssetBundleCache Cache_;
        private readonly List<Action<bool>> AssetLoaderCallbackList_ = new();
        
        public bool IsUsed => RefCount_ > 0;
        private int RefCount_;
        private float RetainTime_;
        private LoadAssetBaseTask LoadAssetTask_;
        
        public AssetInfoCache(AssetBundleCache cache, string assetPath)
        {
            Stage = AssetCacheStage.Created;
            Asset = null;
            
            AssetPath_ = assetPath;
            Cache_ = cache;
            RefCount_ = 0;
            RetainTime_ = 0;
        }

        public void Dispose()
        {
            Unload();

            LoadAssetTask_ = null;
            AssetLoaderCallbackList_.Clear();
            Asset = null;
        }

        public void Unload()
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }
            
            if (RefCount_ > 0 && !(Stage == AssetCacheStage.Retained || Stage == AssetCacheStage.Unloading))
            {
                LLog.Warning($"unload asset leak : {AssetPath_}({RefCount_})");
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
        }

        private void IncRef()
        {
            if (Stage == AssetCacheStage.Retained)
            {
                Stage = AssetCacheStage.Loaded;
            }

            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Created && Stage != AssetCacheStage.Loading)
            {
                LLog.Error($"asset IncRef error, {AssetPath_} : {Stage}");
            }
            
            RefCount_++;
        }

        private void DecRef()
        {
            if (Stage != AssetCacheStage.Loaded)
            {
                LLog.Error($"asset DecRef error, {AssetPath_} : {Stage}");
            }
            
            RefCount_--;
            
            if (RefCount_ <= 0)
            {
                if (LiteRuntime.Setting.Asset.EnableRetain)
                {
                    Stage = AssetCacheStage.Retained;
                    RetainTime_ = LiteRuntime.Setting.Asset.AssetRetainTime;
                }
                else
                {
                    Stage = AssetCacheStage.Unloading;
                    RetainTime_ = 0f;
                }
            }
        }

        private bool OnAssetLoaded(UnityEngine.Object asset)
        {
            Asset = asset;

            if (asset == null)
            {
                Stage = AssetCacheStage.Unloading;
                LLog.Error($"load asset failed : {AssetPath_}");
                return false;
            }

            Stage = AssetCacheStage.Loaded;
            return true;
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