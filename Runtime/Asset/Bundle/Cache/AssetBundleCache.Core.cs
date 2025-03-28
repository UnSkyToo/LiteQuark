using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.AssetBundle Bundle { get; private set; }
        public bool IsLoaded => Stage == AssetCacheStage.Loaded || Stage == AssetCacheStage.Retained;

        private readonly BundleInfo BundleInfo_;
        private readonly AssetBundleProvider Provider_;
        private readonly List<AssetBundleCache> DependencyCacheList_ = new();
        private readonly List<Action<bool>> BundleLoaderCallbackList_ = new ();
        private readonly Dictionary<string, AssetInfoCache> AssetCacheMap_ = new();
        private readonly List<string> UnloadAssetList_ = new();
        
        public bool IsUsed => RefCount_ > 0;
        private int RefCount_;
        private float RetainTime_;
        private LoadBundleBaseTask LoadBundleTask_;

        public AssetBundleCache(AssetBundleProvider provider, BundleInfo bundleInfo)
        {
            Stage = AssetCacheStage.Created;
            Bundle = null;

            BundleInfo_ = bundleInfo;
            Provider_ = provider;
            RefCount_ = 0;
            RetainTime_ = 0;
        }

        public void Dispose()
        {
            Unload(true);
            DependencyCacheList_.Clear();
            BundleLoaderCallbackList_.Clear();
            UnloadAssetList_.Clear();

            if (Bundle != null)
            {
                Bundle.Unload(true);
                Bundle = null;
            }
        }
        
        public void Unload(bool forceMode)
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
            
            if (RefCount_ > 0 && !(Stage == AssetCacheStage.Retained || Stage == AssetCacheStage.Unloading))
            {
                LLog.Warning($"unload bundle leak : {BundleInfo_.BundlePath}({RefCount_})");
            }
            
            foreach (var cache in DependencyCacheList_)
            {
                // force mode unload, ignore ref mode unload
                if (forceMode && cache.Stage == AssetCacheStage.Unloaded)
                {
                    continue;
                }
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
                    UnloadAssetCache(assetPath);
                }
                UnloadAssetList_.Clear();
            }
        }
        
        private void AddDependencyCache(AssetBundleCache cache)
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

            if (Stage != AssetCacheStage.Loaded && Stage != AssetCacheStage.Created && Stage != AssetCacheStage.Loading)
            {
                LLog.Error($"bundle IncRef error, {BundleInfo_.BundlePath} : {Stage}");
            }
            
            RefCount_++;
        }

        private void DecRef()
        {
            if (Stage != AssetCacheStage.Loaded)
            {
                LLog.Error($"bundle DecRef error, {BundleInfo_.BundlePath} : {Stage}");
            }
            
            RefCount_--;

            if (RefCount_ <= 0)
            {
                if (LiteRuntime.Setting.Asset.EnableRetain)
                {
                    Stage = AssetCacheStage.Retained;
                    RetainTime_ = LiteRuntime.Setting.Asset.BundleRetainTime;
                }
                else
                {
                    Stage = AssetCacheStage.Unloading;
                    RetainTime_ = 0f;
                }
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

        private void OnBundleLoaded(UnityEngine.AssetBundle bundle)
        {
            if (bundle != null)
            {
                Bundle = bundle;
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

        public void UnloadSceneAsync(string sceneName, Action callback)
        {
            DecRef();
            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            LiteRuntime.Task.AddTask(op, callback);
        }
        
        public void UnloadUnusedAssets()
        {
            var unloadList = new List<string>();
            
            foreach (var chunk in AssetCacheMap_)
            {
                if (chunk.Value.Stage == AssetCacheStage.Retained || chunk.Value.Stage == AssetCacheStage.Unloading)
                {
                    unloadList.Add(chunk.Key);
                }
            }

            if (unloadList.Count > 0)
            {
                foreach (var assetPath in unloadList)
                {
                    UnloadAssetCache(assetPath);
                }
                unloadList.Clear();
            }
        }
        
        private void UnloadAssetCache(string assetPath)
        {
            if (AssetCacheMap_.ContainsKey(assetPath))
            {
                AssetCacheMap_[assetPath].Unload();
                AssetCacheMap_.Remove(assetPath);
                DecRef();
            }
        }
    }
}