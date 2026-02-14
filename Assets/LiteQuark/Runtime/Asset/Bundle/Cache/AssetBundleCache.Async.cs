using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, assetPath, BundlePath);

            LoadBundleAsync(0, (bundleIsLoaded) =>
            {
                if (!bundleIsLoaded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetPath, BundlePath, false, errorMessage: "Bundle load failed");
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }
                
                var cache = GetOrCreateAssetCache(assetPath);
                cache.LoadAssetAsync<T>((assetIsLoaded) =>
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetPath, BundlePath, assetIsLoaded, errorMessage: assetIsLoaded ? null : "Asset load failed");

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
            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, sceneName, BundlePath);

            LoadBundleAsync(0, (bundleIsLoaded) =>
            {
                if (!bundleIsLoaded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneName, BundlePath, false, errorMessage: "Bundle load failed");
                    LiteUtils.SafeInvoke(callback, false);
                    return;
                }
                
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Scene, sceneName, BundlePath);

                LiteRuntime.Task.LoadSceneTask(sceneName, parameters, (sceneIsLoaded) =>
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Scene, sceneName, BundlePath, sceneIsLoaded, errorMessage: sceneIsLoaded ? null : "Scene load failed");
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneName, BundlePath, sceneIsLoaded, errorMessage: sceneIsLoaded ? null : "Scene load failed");

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
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Bundle, string.Empty, BundlePath, DependencyList, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Bundle, string.Empty, BundlePath, true, FileSize, isCached: true);

                LiteUtils.SafeInvoke(callback, true);
                return;
            }
            
            _bundleLoaderCallbackList.Add(callback);
            if (Stage != AssetCacheStage.Created)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            
            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Bundle, string.Empty, BundlePath, DependencyList);
            
            var dependencies = _bundleInfo.DependencyList ?? Array.Empty<string>();
            var acg = new AsyncCompletionGroup<UnityEngine.AssetBundle>(dependencies.Length + 1, HandleBundleLoadCompleted);
            foreach (var dependency in dependencies)
            {
                var dependencyCache = _provider.GetOrCreateBundleCache(dependency);
                if (dependencyCache == null)
                {
                    LLog.Error("Missing dependency bundle: {0} -> {1}", BundlePath, dependency);
                    acg.SignalSub(false);
                    continue;
                }
                dependencyCache.LoadBundleAsync(priority, acg.SignalSub);
            }
            _provider.LoadBundle(_bundleInfo, priority, acg.SignalMain);
        }
        
        private void HandleBundleLoadCompleted(bool success, UnityEngine.AssetBundle bundle)
        {
            var isLoaded = OnBundleLoaded(success ? bundle : null);
            
            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Bundle, string.Empty, BundlePath, isLoaded, FileSize, errorMessage: isLoaded ? null : "Bundle load failed");
            
            var callbacks = new System.Collections.Generic.List<Action<bool>>(_bundleLoaderCallbackList);
            _bundleLoaderCallbackList.Clear();
            
            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }
        }
    }
}