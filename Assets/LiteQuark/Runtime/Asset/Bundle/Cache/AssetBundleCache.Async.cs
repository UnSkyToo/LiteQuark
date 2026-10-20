using System;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(string assetPath, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object
        {
            var cache = GetOrCreateAssetCache(assetPath);
            var requestLease = cache.AcquireLease<T>();

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, assetPath, BundlePath);

            LoadBundleAsync(0, bundleIsLoaded =>
            {
                if (!bundleIsLoaded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetPath, BundlePath, false, errorMessage: "Bundle load failed");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }

                cache.LoadAssetAsync(requestLease, lease =>
                {
                    var assetIsLoaded = lease != null;
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetPath, BundlePath, assetIsLoaded, errorMessage: assetIsLoaded ? null : "Asset load failed");
                    LiteUtils.SafeInvoke(callback, lease);
                });
            });
        }

        public void LoadSceneAsync(string sceneName, LoadSceneParameters parameters, Action<SceneInfoCache.Lease> callback)
        {
            var requestLease = AcquireLease();

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, sceneName, BundlePath);

            LoadBundleAsync(0, bundleIsLoaded =>
            {
                if (!bundleIsLoaded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneName, BundlePath, false, errorMessage: "Bundle load failed");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }

                var cache = CreateSceneCache(sceneName, parameters, requestLease);
                cache.LoadSceneAsync(lease =>
                {
                    var sceneIsLoaded = lease != null;
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneName, BundlePath, sceneIsLoaded, errorMessage: sceneIsLoaded ? null : "Scene load failed");
                    LiteUtils.SafeInvoke(callback, lease);
                });
            });
        }

        private void LoadBundleAsync(int priority, Action<bool> callback)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, false);
                return;
            }

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

                _dependencyLeases.Add(dependencyCache.AcquireLease());
                dependencyCache.LoadBundleAsync(priority, acg.SignalSub);
            }

            _provider.LoadBundle(_bundleInfo, priority, acg.SignalMain);
        }

        private void HandleBundleLoadCompleted(bool success, UnityEngine.AssetBundle bundle)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                if (bundle != null)
                {
                    bundle.Unload(true);
                }
                return;
            }

            if (!success || bundle == null)
            {
                if (bundle != null)
                {
                    bundle.Unload(true);
                }

                ReleaseDependencyLeases();
                bundle = null;
            }

            var isLoaded = OnBundleLoaded(bundle);

            var callbacks = new System.Collections.Generic.List<Action<bool>>(_bundleLoaderCallbackList);
            _bundleLoaderCallbackList.Clear();

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Bundle, string.Empty, BundlePath, isLoaded, FileSize, errorMessage: isLoaded ? null : "Bundle load failed");

            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }
        }
    }
}
