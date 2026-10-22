using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var assetType = typeof(T);
            var cache = GetOrCreateAssetCache(assetKey, assetType);
            var requestLease = cache.AcquireLease<T>();

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, assetKey, assetType, BundlePath);

            LoadBundleAsync(priority, bundleIsLoaded =>
            {
                if (!bundleIsLoaded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetKey, assetType, BundlePath, false, errorMessage: "Bundle load failed");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }

                cache.LoadAssetAsync(requestLease, lease =>
                {
                    var assetIsLoaded = lease != null;
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetKey, assetType, BundlePath, assetIsLoaded, errorMessage: assetIsLoaded ? null : "Asset load failed");
                    LiteUtils.SafeInvoke(callback, lease);
                });
            });
        }

        public void LoadSceneAsync(string sceneKey, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority, Action<SceneInfoCache.Lease> callback)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, null);
                return;
            }

            var assetType = typeof(UnityEngine.SceneManagement.Scene);
            var requestLease = AcquireLease();

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, sceneKey, assetType, BundlePath);

            LoadBundleAsync(priority, bundleIsLoaded =>
            {
                if (!bundleIsLoaded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneKey, assetType, BundlePath, false, errorMessage: "Bundle load failed");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }

                if (!TryGetSceneLoadPath(sceneKey, out var loadPath))
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneKey, assetType, BundlePath, false, errorMessage: "Scene path not found in bundle");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, null);
                    return;
                }

                var cache = CreateSceneCache(sceneKey, loadPath, parameters, requestLease);
                cache.LoadSceneAsync(lease =>
                {
                    var sceneIsLoaded = lease != null;
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneKey, assetType, BundlePath, sceneIsLoaded, errorMessage: sceneIsLoaded ? null : "Scene load failed");
                    LiteUtils.SafeInvoke(callback, lease);
                });
            });
        }

        private void LoadBundleAsync(AssetLoadPriority priority, Action<bool> callback)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, false);
                return;
            }

            if (IsLoaded)
            {
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Bundle, string.Empty, null, BundlePath, DependencyList, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Bundle, string.Empty, null, BundlePath, true, FileSize, isCached: true);

                LiteUtils.SafeInvoke(callback, true);
                return;
            }

            _bundleLoaderCallbackList.Add(callback);

            if (Stage == AssetCacheStage.Loading)
            {
                RaiseLoadPriority(priority);
                return;
            }

            _loadPriority = priority;
            Stage = AssetCacheStage.Loading;

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Bundle, string.Empty, null, BundlePath, DependencyList);

            var dependencies = DependencyList;
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
                dependencyCache.LoadBundleAsync(_loadPriority, acg.SignalSub);
            }

            _provider.LoadBundle(_bundleInfo, _loadPriority, acg.SignalMain);
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

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Bundle, string.Empty, null, BundlePath, isLoaded, FileSize, errorMessage: isLoaded ? null : "Bundle load failed");

            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, isLoaded);
            }
        }
    }
}
