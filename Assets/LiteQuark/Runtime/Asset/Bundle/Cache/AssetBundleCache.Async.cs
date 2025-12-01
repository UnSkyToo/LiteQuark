using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            LoadBundleAsync(0, (bundleIsLoaded) =>
            {
                if (!bundleIsLoaded)
                {
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }
                
                var cache = GetOrCreateAssetCache(assetPath);
                cache.LoadAssetAsync<T>((assetIsLoaded) =>
                {
                    if (assetIsLoaded)
                    {
                        IncRef();
                    }
                
                    LiteUtils.SafeInvoke(callback, cache.Asset as T);
                });
            });
        }

        public void LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            LoadBundleAsync(0, (bundleIsLoaded) =>
            {
                if (!bundleIsLoaded)
                {
                    LiteUtils.SafeInvoke(callback, false);
                    return;
                }
                
                LiteRuntime.Task.LoadSceneTask(sceneName, parameters, (sceneIsLoaded) =>
                {
                    if (sceneIsLoaded)
                    {
                        IncRef();
                    }

                    LiteUtils.SafeInvoke(callback, sceneIsLoaded);
                });
            });
        }
        
        private void LoadBundleAsync(int priority, Action<bool> callback)
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
            
            var dependencies = _bundleInfo.DependencyList ?? Array.Empty<string>();
            var acg = new AsyncCompletionGroup<UnityEngine.AssetBundle>(dependencies.Length + 1, HandleBundleLoadCompleted);
            foreach (var dependency in dependencies)
            {
                var dependencyCache = _provider.GetOrCreateBundleCache(dependency);
                dependencyCache.LoadBundleAsync(priority, acg.SignalSub);
            }
            _provider.LoadBundle(_bundleInfo, priority, acg.SignalMain);
        }
        
        private void HandleBundleLoadCompleted(bool success, UnityEngine.AssetBundle bundle)
        {
            var isLoaded = success && OnBundleLoaded(bundle);
            
            foreach (var loader in _bundleLoaderCallbackList)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }

            _bundleLoaderCallbackList.Clear();
        }
    }
}