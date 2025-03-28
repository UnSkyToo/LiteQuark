﻿#if LITE_QUARK_ASSET_ENABLE_SYNC
namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public bool LoadBundleCompleteSync()
        {
            if (IsLoaded)
            {
                return true;
            }

            var isLoaded = LoadBundleDependenciesSync();
            if (!isLoaded)
            {
                return false;
            }
            
            return LoadBundleSync();
        }

        private bool LoadBundleDependenciesSync()
        {
            var dependencies = BundleInfo_.DependencyList;
            if (dependencies == null || dependencies.Length == 0)
            {
                return true;
            }

            foreach (var dependency in dependencies)
            {
                var dependencyCache = Loader_.GetOrCreateBundleCache(dependency);
                var isLoaded = dependencyCache.LoadBundleCompleteSync();
                if (!isLoaded)
                {
                    return false;
                }

                AddDependencyCache(dependencyCache);
            }

            return true;
        }
        
        private bool LoadBundleSync()
        {
            if (IsLoaded)
            {
                return true;
            }

            if (Stage == AssetCacheStage.Loading)
            {
                return ForceLoadBundleComplete();
            }

            Stage = AssetCacheStage.Loading;
            var fullPath = Loader_.GetBundleUri(BundleInfo_);
            var bundle = UnityEngine.AssetBundle.LoadFromFile(fullPath);

            if (bundle != null)
            {
                OnBundleLoaded(bundle);
                return true;
            }
            else
            {
                Stage = AssetCacheStage.Invalid;
                LLog.Error($"load bundle failed : {BundleInfo_.BundlePath}");
                return false;
            }
        }
        
        private bool ForceLoadBundleComplete()
        {
            if (IsLoaded)
            {
                return true;
            }

            foreach (var bundle in DependencyCacheList_)
            {
                if (!bundle.ForceLoadBundleComplete())
                {
                    return false;
                }
            }

            if (LoadBundleTask_ != null)
            {
                var bundle = LoadBundleTask_.WaitCompleted();
                return bundle != null;
            }

            return true;
        }
        
        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var cache = GetOrCreateAssetCache(assetPath);
            var asset = cache.LoadAssetSync<T>();
            
            if (asset != null)
            {
                IncRef();
            }
            
            return asset;
        }

        public bool LoadSceneSync(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, parameters);
            if (scene.isLoaded)
            {
                IncRef();
            }
            return scene.isLoaded;
        }
    }
}
#endif