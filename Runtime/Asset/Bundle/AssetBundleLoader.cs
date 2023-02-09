using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    internal sealed class AssetBundleLoader : IAssetLoader
    { 
        private BundlePackInfo PackInfo_ = null;
        private readonly Dictionary<int, AssetBundleCache> AssetIDToBundleCache_ = new();
        private readonly Dictionary<string, AssetBundleCache> PathToBundleCache_ = new();
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
            
            AssetIDToBundleCache_.Clear();
            PathToBundleCache_.Clear();
            BundleCacheLoaderCallback_.Clear();
            
            return true;
        }
        
        public void Dispose()
        {
            BundleCacheLoaderCallback_.Clear();
            foreach (var bundle in PathToBundleCache_)
            {
                bundle.Value.Unload();
            }
            PathToBundleCache_.Clear();
            AssetIDToBundleCache_.Clear();
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
                
                cache.LoadAsset<T>(assetPath, (isLoaded) =>
                {
                    if (!isLoaded)
                    {
                        LLog.Error($"load asset error : {assetPath}");
                        callback?.Invoke(null);
                        return;
                    }
                    
                    var asset = cache.CreateAsset<T>(assetPath);
                    if (asset != null)
                    {
                        if (!AssetIDToBundleCache_.ContainsKey(asset.GetInstanceID()))
                        {
                            AssetIDToBundleCache_.Add(asset.GetInstanceID(), cache);
                        }
                    
                        callback?.Invoke(asset);
                        return;
                    }
                    
                    callback?.Invoke(null);
                });
            });
        }

        public void UnloadAsset(string assetPath)
        {
            var info = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (info == null)
            {
                return;
            }

            if (PathToBundleCache_.TryGetValue(info.BundlePath, out var cache) && cache.IsLoad)
            {
                cache.UnloadAsset(assetPath);
            }
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset == null)
            {
                return;
            }

            var instanceID = asset.GetInstanceID();
            if (!AssetIDToBundleCache_.ContainsKey(instanceID))
            {
                return;
            }

            AssetIDToBundleCache_[instanceID].DecRef();
            AssetIDToBundleCache_.Remove(instanceID);
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

            if (!PathToBundleCache_.TryGetValue(info.BundlePath, out var cache))
            {
                cache = new AssetBundleCache(info);
                PathToBundleCache_.Add(info.BundlePath, cache);
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
                    PathToBundleCache_.Remove(cache.Info.BundlePath);
                    LLog.Error($"load bundle error : {cache.Info.BundlePath}");
                    callback?.Invoke(null);
                    return;
                }
                
                callback?.Invoke(cache);
            });
        }
    }
}