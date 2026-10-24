using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetLoadResult<AssetInfoCache.Lease<T>>> callback) where T : UnityEngine.Object
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<AssetInfoCache.Lease<T>>.Failure(new AssetError(assetKey, "Bundle cache is unloading or unloaded")));
                return;
            }

            var assetType = typeof(T);
            var cache = GetOrCreateAssetCache(assetKey, assetType);
            var requestLease = cache.AcquireLease<T>();

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, assetKey, assetType, BundlePath);

            LoadBundleAsync(priority, bundleResult =>
            {
                if (!bundleResult.Succeeded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetKey, assetType, BundlePath, false, errorMessage: "Bundle load failed");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<AssetInfoCache.Lease<T>>.Failure(bundleResult.Error));
                    return;
                }

                cache.LoadAssetAsync(requestLease, result =>
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, assetKey, assetType, BundlePath, result.Succeeded, errorMessage: result.Succeeded ? null : "Asset load failed");
                    LiteUtils.SafeInvoke(callback, result);
                });
            });
        }

        public void LoadSceneAsync(string sceneKey, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority, Action<AssetLoadResult<SceneInfoCache.Lease>> callback)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<SceneInfoCache.Lease>.Failure(new AssetError(sceneKey, "Bundle cache is unloading or unloaded")));
                return;
            }

            var assetType = typeof(UnityEngine.SceneManagement.Scene);
            var requestLease = AcquireLease();

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Session, sceneKey, assetType, BundlePath);

            LoadBundleAsync(priority, bundleResult =>
            {
                if (!bundleResult.Succeeded)
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneKey, assetType, BundlePath, false, errorMessage: "Bundle load failed");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<SceneInfoCache.Lease>.Failure(bundleResult.Error));
                    return;
                }

                if (!TryGetSceneLoadPath(sceneKey, out var loadPath, out var pathError))
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneKey, assetType, BundlePath, false, errorMessage: "Scene path not found in bundle");
                    requestLease.Dispose();
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<SceneInfoCache.Lease>.Failure(pathError));
                    return;
                }

                var cache = CreateSceneCache(sceneKey, loadPath, parameters, requestLease);
                cache.LoadSceneAsync(result =>
                {
                    AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Session, sceneKey, assetType, BundlePath, result.Succeeded, errorMessage: result.Succeeded ? null : "Scene load failed");
                    LiteUtils.SafeInvoke(callback, result);
                });
            });
        }

        private void LoadBundleAsync(AssetLoadPriority priority, Action<AssetLoadResult<UnityEngine.AssetBundle>> callback)
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
            {
                LiteUtils.SafeInvoke(callback, AssetLoadResult<UnityEngine.AssetBundle>.Failure(new AssetError(BundlePath, "Bundle cache is unloading or unloaded")));
                return;
            }

            if (IsLoaded)
            {
                AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Bundle, string.Empty, null, BundlePath, DependencyList, isCached: true);
                AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Bundle, string.Empty, null, BundlePath, true, FileSize, isCached: true);

                LiteUtils.SafeInvoke(callback, AssetLoadResult<UnityEngine.AssetBundle>.Success(Bundle));
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
            AssetError firstError = null;
            var acg = new AsyncCompletionGroup<UnityEngine.AssetBundle>(dependencies.Length + 1,
                (success, bundle) => HandleBundleLoadCompleted(success, bundle, firstError));

            foreach (var dependency in dependencies)
            {
                var dependencyCache = _provider.GetOrCreateBundleCache(dependency);
                if (dependencyCache == null)
                {
                    LLog.Error("Missing dependency bundle: {0} -> {1}", BundlePath, dependency);
                    firstError ??= new AssetError(dependency, "Dependency bundle is not in the manifest");
                    acg.SignalSub(false);
                    continue;
                }

                _dependencyLeases.Add(dependencyCache.AcquireLease());
                dependencyCache.LoadBundleAsync(_loadPriority, result =>
                {
                    firstError ??= result.Error;
                    acg.SignalSub(result.Succeeded);
                });
            }

            _provider.LoadBundle(_bundleInfo, _loadPriority, result =>
            {
                firstError ??= result.Error;
                acg.SignalMain(result.Value);
            });
        }

        private void HandleBundleLoadCompleted(bool success, UnityEngine.AssetBundle bundle, AssetError error)
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

            if (!isLoaded)
            {
                error ??= new AssetError(BundlePath, "Bundle load returned no result");
            }

            var result = isLoaded
                ? AssetLoadResult<UnityEngine.AssetBundle>.Success(bundle)
                : AssetLoadResult<UnityEngine.AssetBundle>.Failure(error);
            var callbacks = new System.Collections.Generic.List<Action<AssetLoadResult<UnityEngine.AssetBundle>>>(_bundleLoaderCallbackList);
            _bundleLoaderCallbackList.Clear();

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Bundle, string.Empty, null, BundlePath, isLoaded, FileSize, errorMessage: isLoaded ? null : "Bundle load failed");

            foreach (var loader in callbacks)
            {
                LiteUtils.SafeInvoke(loader, result);
            }
        }
    }
}
