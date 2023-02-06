using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    internal sealed class AssetBundleLoader : IAssetLoader
    { 
        private BundlePackInfo PackInfo_ = null;
        private readonly Dictionary<string, AssetBundleCache> BundleCache_ = new();
        private readonly Dictionary<string, List<Action<AssetBundleCache>>> BundleCacheLoaderCallback_ = new();

        public AssetBundleLoader()
        {
        }
        
        public bool Initialize()
        {
            PackInfo_ = LoadBundlePack();
            
            if (PackInfo_ == null)
            {
                return false;
            }
            
            BundleCache_.Clear();
            BundleCacheLoaderCallback_.Clear();
            
            return true;
        }
        
        public void Dispose()
        {
            BundleCacheLoaderCallback_.Clear();
            foreach (var bundle in BundleCache_)
            {
                bundle.Value.Unload();
            }
            BundleCache_.Clear();
        }

        private BundlePackInfo LoadBundlePack()
        {
            var request = UnityWebRequest.Get(PathUtils.ConcatPath(Application.streamingAssetsPath, LiteConst.BundlePackFileName));
            request.SendWebRequest();
            while (!request.isDone)
            {
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                LLog.Error($"load bundle package error\n{request.error}");
                return null;
            }

            var info = BundlePackInfo.FromJson(request.downloadHandler.text);
            
            if (info is not {IsValid: true})
            {
                return null;
            }
            
            info.Initialize();
            return info;
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            LoadBundleCacheAsync(assetPath, (cache) =>
            {
                callback?.Invoke(cache != null);
            });
        }

        public void StopLoadAsset(string assetPath)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            BundleCacheLoaderCallback_.Remove(info.BundlePath);
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            LoadBundleCacheAsync(assetPath, (cache) =>
            {
                if (cache == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                cache.LoadAsset(assetPath, callback);
            });
        }

        public void UnloadAsset(string assetPath)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            if (BundleCache_.TryGetValue(info.BundlePath, out var cache) && cache.IsLoad)
            {
                cache.DecRef();
            }
        }

        private void LoadBundleCacheAsync(string assetPath, Action<AssetBundleCache> callback)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            LoadBundleCacheAsync(info, callback);
        }

        private void LoadBundleCacheAsync(BundleInfo info, Action<AssetBundleCache> callback)
        {
            if (info == null)
            {
                callback?.Invoke(null);
                return;
            }

            if (!BundleCache_.TryGetValue(info.BundlePath, out var cache))
            {
                cache = new AssetBundleCache(info);
                BundleCache_.Add(info.BundlePath, cache);
            }
            
            LoadBundleCacheAsync(cache, callback);
        }

        private void LoadBundleCacheAsync(AssetBundleCache cache, Action<AssetBundleCache> callback)
        {
            if (cache.IsLoad)
            {
                callback?.Invoke(cache);
                return;
            }
            
            if (BundleCacheLoaderCallback_.TryGetValue(cache.Info.BundlePath, out var list))
            {
                list.Add(callback);
                return;
            }

            BundleCacheLoaderCallback_.Add(cache.Info.BundlePath, new List<Action<AssetBundleCache>> {callback});
            
            LoadDependencyBundleCache(cache, (isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }

                LoadBundleCacheInternal(cache, (loadBundle) =>
                {
                    foreach (var loader in BundleCacheLoaderCallback_[cache.Info.BundlePath])
                    {
                        loader?.Invoke(loadBundle);
                    }
                    
                    BundleCacheLoaderCallback_.Remove(cache.Info.BundlePath);
                });
            });
        }
        
        private void LoadDependencyBundleCache(AssetBundleCache cache, Action<bool> callback)
        {
            var loadCompletedCount = 0;
            var dependencyPathList = cache.GetDependencyPathList();

            if (dependencyPathList.Length == 0)
            {
                callback?.Invoke(true);
                return;
            }

            foreach (var dependencyPath in dependencyPathList)
            {
                var dependencyInfo = PackInfo_.GetBundleInfoFromBundlePath(dependencyPath);
                LoadBundleCacheAsync(dependencyInfo, (dependencyBundle) =>
                {
                    if (dependencyBundle == null)
                    {
                        callback?.Invoke(false);
                        return;
                    }
                    
                    cache.AddDependencyCache(dependencyBundle);
                    loadCompletedCount++;

                    if (loadCompletedCount >= dependencyPathList.Length)
                    {
                        callback?.Invoke(true);
                    }
                });
            }
        }

        private void LoadBundleCacheInternal(AssetBundleCache cache, Action<AssetBundleCache> callback)
        {
            cache.Load((isLoaded) =>
            {
                if (!isLoaded)
                {
                    BundleCache_.Remove(cache.Info.BundlePath);
                    LLog.Error($"load bundle error : {cache.Info.BundlePath}");
                    callback?.Invoke(null);
                    return;
                }
                
                callback?.Invoke(cache);
            });
        }
    }
}