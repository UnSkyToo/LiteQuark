using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        internal void LoadBundleAsync(Action<bool> callback)
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
            
            if (Provider_.IsEnableRemoteBundle())
            {
                var bundleUri = Provider_.GetBundleUri(BundleInfo_);
                LoadBundleTask_ = LiteRuntime.Task.LoadRemoteBundleTask(bundleUri, HandleBundleLoadCompleted);
            }
            else
            {
                var bundleUri = Provider_.GetBundleUri(BundleInfo_);
                LoadBundleTask_ = LiteRuntime.Task.LoadLocalBundleTask(bundleUri, HandleBundleLoadCompleted);
            }

            LoadDependencyBundleAsync(LoadBundleTask_);
        }

        private void HandleBundleLoadCompleted(UnityEngine.AssetBundle bundle)
        {
            LoadBundleTask_ = null;

            var isLoaded = OnBundleLoaded(bundle);

            foreach (var loader in BundleLoaderCallbackList_)
            {
                loader?.Invoke(isLoaded);
            }

            BundleLoaderCallbackList_.Clear();
        }

        private void LoadDependencyBundleAsync(LoadBundleBaseTask mainTask)
        {
            var dependencies = BundleInfo_.DependencyList ?? Array.Empty<string>();
            
            foreach (var dependency in dependencies)
            {
                var dependencyCache = Provider_.GetOrCreateBundleCache(dependency);
                dependencyCache.LoadBundleAsync(null);
                mainTask.AddChildTask(dependencyCache.LoadBundleTask_);
            }
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