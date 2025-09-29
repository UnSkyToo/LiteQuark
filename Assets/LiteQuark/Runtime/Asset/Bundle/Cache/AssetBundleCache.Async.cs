using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        internal void LoadBundleAsync(int priority, Action<bool> callback)
        {
            if (IsLoaded)
            {
                LiteUtils.SafeInvoke(callback, true);
                return;
            }
            
            _bundleLoaderCallbackList.Add(callback);
            if (Stage == AssetCacheStage.Loading)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            LoadDependencyBundleAsync(priority + 1);
            _provider.LoadBundle(_bundleInfo, priority, HandleBundleLoadCompleted);
        }

        private void HandleBundleLoadCompleted(UnityEngine.AssetBundle bundle)
        {
            var isLoaded = OnBundleLoaded(bundle);

            foreach (var loader in _bundleLoaderCallbackList)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }

            _bundleLoaderCallbackList.Clear();
        }

        private void LoadDependencyBundleAsync(int priority)
        {
            var dependencies = _bundleInfo.DependencyList ?? Array.Empty<string>();

            foreach (var dependency in dependencies)
            {
                var dependencyCache = _provider.GetOrCreateBundleCache(dependency);
                dependencyCache.LoadBundleAsync(priority, null);
            }
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var cache = GetOrCreateAssetCache(assetPath);
            cache.LoadAssetAsync<T>((isLoaded) =>
            {
                if (isLoaded)
                {
                    IncRef();
                }
                
                LiteUtils.SafeInvoke(callback, cache.Asset as T);
            });
        }

        public void LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, parameters);
            if (op == null)
            {
                LiteUtils.SafeInvoke(callback, false);
                return;
            }
            
            op.completed += (result) =>
            {
                if (result.isDone)
                {
                    IncRef();
                }
                LiteUtils.SafeInvoke(callback, result.isDone);
            };
        }
    }
}