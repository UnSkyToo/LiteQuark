using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleLoader : IAssetLoader
    {
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                callback?.Invoke(null);
                return;
            }

            LoadBundleAsync(info, (isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }

                var cache = BundleCacheMap_[info.BundlePath];
                cache.LoadAssetAsync<T>(assetPath, (asset) =>
                {
                    if (asset != null)
                    {
                        AssetIDToPathMap_.TryAdd(asset.GetInstanceID(), assetPath);
                    }
                    callback?.Invoke(asset);
                });
            });
        }

        private void LoadBundleAsync(BundleInfo info, Action<bool> callback)
        {
            if (BundleExisted(info))
            {
                callback?.Invoke(true);
                return;
            }

            if (BundleLoaderCallbackList_.TryGetValue(info.BundlePath, out var list))
            {
                list.Add(callback);
                return;
            }

            BundleLoaderCallbackList_.Add(info.BundlePath, new List<Action<bool>> { callback });
            LoadBundleCompleteAsync(info, (isLoaded) =>
            {
                if (!isLoaded)
                {
                    BundleCacheMap_.Remove(info.BundlePath);
                }
                
                foreach (var loadCallback in BundleLoaderCallbackList_[info.BundlePath])
                {
                    loadCallback?.Invoke(isLoaded);
                }

                BundleLoaderCallbackList_.Remove(info.BundlePath);
            });
        }

        private void LoadBundleCompleteAsync(BundleInfo info, Action<bool> callback)
        {
            if (!BundleCacheMap_.TryGetValue(info.BundlePath, out var cache))
            {
                cache = new AssetBundleCache(info);
                BundleCacheMap_.Add(info.BundlePath, cache);
            }
            
            LoadBundleDependenciesAsync(cache, (isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(false);
                    return;
                }
                
                cache.LoadBundleAsync(callback);
            });
        }

        private void LoadBundleDependenciesAsync(AssetBundleCache cache, Action<bool> callback)
        {
            var dependencies = cache.GetAllDependencies();
            if (dependencies == null || dependencies.Length == 0)
            {
                callback?.Invoke(true);
                return;
            }

            var loadCompletedCount = 0;
            foreach (var dependency in dependencies)
            {
                var dependencyInfo = PackInfo_.GetBundleInfoFromBundlePath(dependency);
                LoadBundleAsync(dependencyInfo, (isLoaded) =>
                {
                    if (!isLoaded)
                    {
                        callback?.Invoke(false);
                        return;
                    }

                    cache.AddDependencyCache(BundleCacheMap_[dependencyInfo.BundlePath]);
                    loadCompletedCount++;

                    if (loadCompletedCount >= dependencies.Length)
                    {
                        callback?.Invoke(true);
                    }
                });
            }
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset, parent);
                if (instance != null)
                {
                    AssetIDToPathMap_.TryAdd(instance.GetInstanceID(), assetPath);
                }
                callback?.Invoke(instance);
            });
        }
    }
}