﻿using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadBundleCompleteAsync(Action<bool> callback)
        {
            if (Stage == AssetCacheStage.Loaded)
            {
                callback?.Invoke(true);
                return;
            }
            
            var info = Loader_.GetPackInfo().GetBundleInfoFromBundlePath(BundlePath_);
            if (info == null)
            {
                callback?.Invoke(false);
                return;
            }

            LoadBundleDependenciesAsync(info, (isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(false);
                    return;
                }
                
                LoadBundleAsync(callback);
            });
        }
        
        private void LoadBundleDependenciesAsync(BundleInfo info, Action<bool> callback)
        {
            var dependencies = info.DependencyList;
            if (dependencies == null || dependencies.Length == 0)
            {
                callback?.Invoke(true);
                return;
            }

            var loadCompletedCount = 0;
            foreach (var dependency in dependencies)
            {
                var dependencyCache = Loader_.GetOrCreateBundleCache(dependency);
                dependencyCache.LoadBundleCompleteAsync((isLoaded) =>
                {
                    if (!isLoaded)
                    {
                        callback?.Invoke(false);
                        return;
                    }

                    AddDependencyCache(dependencyCache);
                    loadCompletedCount++;

                    if (loadCompletedCount >= dependencies.Length)
                    {
                        callback?.Invoke(true);
                    }
                });
            }
        }
        
        private void LoadBundleAsync(Action<bool> callback)
        {
            if (Stage == AssetCacheStage.Loaded)
            {
                callback?.Invoke(true);
                return;
            }

            BundleLoaderCallbackList_.Add(callback);
            if (Stage == AssetCacheStage.Loading)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var fullPath = PathUtils.GetFullPathInRuntime(BundlePath_);
            BundleRequest_ = UnityEngine.AssetBundle.LoadFromFileAsync(fullPath);
            BundleRequest_.completed += OnBundleRequestCompleted;
        }
        
        private void OnBundleRequestCompleted(UnityEngine.AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;

            var bundle = (op as UnityEngine.AssetBundleCreateRequest)?.assetBundle;
            if (bundle != null)
            {
                OnBundleLoaded(bundle);
                
                foreach (var loader in BundleLoaderCallbackList_)
                {
                    loader?.Invoke(true);
                }
            }
            else
            {
                Stage = AssetCacheStage.Invalid;
                LLog.Error($"load bundle failed : {BundlePath_}");
                
                foreach (var loader in BundleLoaderCallbackList_)
                {
                    loader?.Invoke(false);
                }
            }
            
            BundleLoaderCallbackList_.Clear();
        }
        
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var cache = GetOrCreateAssetCache(assetPath);
            cache.LoadAssetAsync<T>((asset) =>
            {
                if (asset != null)
                {
                    IncRef();
                }
                
                callback?.Invoke(asset);
            });
        }
    }
}