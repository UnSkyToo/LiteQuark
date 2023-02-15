using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed class AssetBundleLoader : IAssetLoader
    {
        private BundlePackInfo PackInfo_ = null;
        
        private readonly Dictionary<string, AssetBundleCache> BundleCacheMap_ = new();
        private readonly Dictionary<string, List<Action<bool>>> BundleLoaderCallbackList_ = new();
        private readonly Dictionary<int, string> AssetIDToPathMap_ = new();

        public AssetBundleLoader()
        {
        }

        public bool Initialize()
        {
            PackInfo_ = BundlePackInfo.LoadBundlePack();
            if (PackInfo_ == null)
            {
                return false;
            }
            
            BundleCacheMap_.Clear();
            BundleLoaderCallbackList_.Clear();
            AssetIDToPathMap_.Clear();
            
            return true;
        }

        public void Dispose()
        {
            AssetIDToPathMap_.Clear();
            BundleLoaderCallbackList_.Clear();
            foreach (var bundle in BundleCacheMap_)
            {
                bundle.Value.Dispose();
            }
            BundleCacheMap_.Clear();
        }

        private bool BundleExisted(BundleInfo info)
        {
            return BundleCacheMap_.TryGetValue(info.BundlePath, out var cache) && cache.IsLoaded;
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            LoadAssetAsync<T>(assetPath, (asset) =>
            {
                callback?.Invoke(asset != null);
            });
        }

        public void StopLoadAsset(string assetPath)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            BundleLoaderCallbackList_.Remove(info.BundlePath);
        }

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
                    cache.IncRef();
                    if (asset != null && !AssetIDToPathMap_.ContainsKey(asset.GetInstanceID()))
                    {
                        AssetIDToPathMap_.Add(asset.GetInstanceID(), assetPath);
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

        public void InstantiateAsync(string assetPath, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset);
                if (instance != null && !AssetIDToPathMap_.ContainsKey(instance.GetInstanceID()))
                {
                    AssetIDToPathMap_.Add(instance.GetInstanceID(), assetPath);
                }
                callback?.Invoke(instance);
            });
        }

        public void UnloadAsset(string assetPath)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            if (BundleCacheMap_.TryGetValue(info.BundlePath, out var cache) && cache.IsLoaded)
            {
                cache.DecRef();
            }
        }
        
        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset == null)
            {
                return;
            }

            var instanceID = asset.GetInstanceID();
            if (AssetIDToPathMap_.TryGetValue(instanceID, out var assetPath))
            {
                UnloadAsset(assetPath);
                AssetIDToPathMap_.Remove(instanceID);
            }

            if (asset is UnityEngine.GameObject)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        public void UnloadUnusedBundle()
        {
            foreach (var bundle in BundleCacheMap_)
            {
                if (bundle.Value.IsLoaded && !bundle.Value.IsUsed)
                {
                    bundle.Value.Unload();
                }
            }

            var disposeList = new List<AssetBundleCache>();
            foreach (var bundle in BundleCacheMap_)
            {
                if (bundle.Value.IsLoaded && !bundle.Value.IsUsed)
                {
                    disposeList.Add(bundle.Value);
                }
            }

            if (disposeList.Count > 0)
            {
                foreach (var cache in disposeList)
                {
                    foreach (var asset in cache.GetLoadAssetList())
                    {
                        AssetIDToPathMap_.Remove(asset.GetInstanceID());
                    }

                    BundleCacheMap_.Remove(cache.Info.BundlePath);
                    cache.Dispose();
                }
                disposeList.Clear();
            }
            
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}