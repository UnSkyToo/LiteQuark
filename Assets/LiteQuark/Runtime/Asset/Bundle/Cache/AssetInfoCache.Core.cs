using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.Object Asset { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded || Stage == AssetCacheStage.Retained;

        private readonly string _assetPath;
        private readonly AssetBundleCache _cache;
        private readonly List<Action<bool>> _assetLoaderCallbackList = new();
        
        public bool IsUsed => _refCount > 0;
        private int _refCount;
        private float _retainTime;
        private BaseTask _loadAssetTask;
        
        public AssetInfoCache(AssetBundleCache cache, string assetPath)
        {
            Stage = AssetCacheStage.Created;
            Asset = null;
            
            _assetPath = assetPath;
            _cache = cache;
            _refCount = 0;
            _retainTime = 0;
        }

        public void Dispose()
        {
            Unload();

            _loadAssetTask = null;
            _assetLoaderCallbackList.Clear();
            Asset = null;
        }

        public void Unload()
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }
            
            if (_refCount > 0 && !(Stage == AssetCacheStage.Retained || Stage == AssetCacheStage.Unloading))
            {
                LLog.Warning("Unload asset leak : {0}({1})", _assetPath, _refCount);
            }

            _refCount = 0;
            Stage = AssetCacheStage.Unloaded;
        }
        
        public void Tick(float deltaTime)
        {
            if (Stage == AssetCacheStage.Retained)
            {
                _retainTime -= deltaTime;
                if (_retainTime <= 0f)
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
                LLog.Error("Asset IncRef error, {0} : {1}", _assetPath, Stage);
            }
            
            _refCount++;
        }

        private void DecRef()
        {
            if (Stage != AssetCacheStage.Loaded)
            {
                LLog.Error("Asset DecRef error, {0} : {1}", _assetPath, Stage);
            }
            
            _refCount--;
            
            if (_refCount <= 0)
            {
                if (LiteRuntime.Setting.Asset.EnableRetain)
                {
                    Stage = AssetCacheStage.Retained;
                    _retainTime = LiteRuntime.Setting.Asset.AssetRetainTime;
                }
                else
                {
                    Stage = AssetCacheStage.Unloading;
                    _retainTime = 0f;
                }
            }
        }

        private bool OnAssetLoaded(UnityEngine.Object asset)
        {
            Asset = asset;

            if (asset == null)
            {
                Stage = AssetCacheStage.Unloading;
                LLog.Error("Load asset failed : {0}", _assetPath);
                return false;
            }

            Stage = AssetCacheStage.Loaded;
            return true;
        }

        public void UnloadAsset(string assetPath)
        {
            if (_assetPath != assetPath)
            {
                return;
            }
            
            DecRef();
        }
    }
}