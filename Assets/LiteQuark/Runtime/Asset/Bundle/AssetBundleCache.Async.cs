using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadBundleCompleteAsync(Action<bool> callback)
        {
            if (IsLoaded)
            {
                callback?.Invoke(true);
                return;
            }

            LoadBundleDependenciesAsync((isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(false);
                    return;
                }
                
                LoadBundleAsync(callback);
            });
        }
        
        private void LoadBundleDependenciesAsync(Action<bool> callback)
        {
            var dependencies = BundleInfo_.DependencyList;
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
            if (IsLoaded)
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
            
            if (Loader_.IsEnableRemoteBundle())
            {
                var bundleUri = Loader_.GetBundleUri(BundleInfo_);
                LoadBundleTask_ = LiteRuntime.Task.LoadRemoteBundleTask(bundleUri, HandleBundleLoadCompleted);
            }
            else
            {
                var bundleUri = Loader_.GetBundleUri(BundleInfo_);
                LoadBundleTask_ = LiteRuntime.Task.LoadLocalBundleTask(bundleUri, HandleBundleLoadCompleted);
            }
        }

        private void HandleBundleLoadCompleted(UnityEngine.AssetBundle bundle)
        {
            LoadBundleTask_ = null;
            
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
                LLog.Error($"load bundle failed : {BundleInfo_.BundlePath}");
                
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

        public void LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, parameters);
            if (op == null)
            {
                callback?.Invoke(false);
                return;
            }
            
            op.completed += (result) =>
            {
                if (result.isDone)
                {
                    IncRef();
                }
                callback?.Invoke(result.isDone);
            };
        }
    }
}