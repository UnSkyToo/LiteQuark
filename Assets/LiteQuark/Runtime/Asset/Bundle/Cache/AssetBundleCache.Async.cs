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
            
            _bundleLoaderCallbackList.Add(callback);
            if (Stage == AssetCacheStage.Loading)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            
            if (_provider.IsEnableRemoteBundle())
            {
                var bundleUri = _provider.GetBundleUri(_bundleInfo);
                _loadBundleTask = LiteRuntime.Task.LoadRemoteBundleTask(bundleUri, HandleBundleLoadCompleted);
            }
            else
            {
                var bundleUri = _provider.GetBundleUri(_bundleInfo);
                _loadBundleTask = LiteRuntime.Task.LoadLocalBundleTask(bundleUri, HandleBundleLoadCompleted);
            }

            LoadDependencyBundleAsync(_loadBundleTask);
        }

        private void HandleBundleLoadCompleted(UnityEngine.AssetBundle bundle)
        {
            _loadBundleTask = null;

            var isLoaded = OnBundleLoaded(bundle);

            foreach (var loader in _bundleLoaderCallbackList)
            {
                loader?.Invoke(isLoaded);
            }

            _bundleLoaderCallbackList.Clear();
        }

        private void LoadDependencyBundleAsync(LoadBundleBaseTask mainTask)
        {
            var dependencies = _bundleInfo.DependencyList ?? Array.Empty<string>();
            
            foreach (var dependency in dependencies)
            {
                var dependencyCache = _provider.GetOrCreateBundleCache(dependency);
                dependencyCache.LoadBundleAsync(null);
                mainTask.AddChildTask(dependencyCache._loadBundleTask);
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