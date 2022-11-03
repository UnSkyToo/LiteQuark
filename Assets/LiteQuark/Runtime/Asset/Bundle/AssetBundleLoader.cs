using System;
using System.Collections.Generic;
using LiteQuark.Runtime.Internal;
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
            var request = UnityWebRequest.Get(PathHelper.ConcatPath(Application.streamingAssetsPath, LiteConst.BundlePackFileName));
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
            LoadBundleCache(assetPath, (bundleCache) =>
            {
                callback?.Invoke(bundleCache != null);
            });
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            LoadBundleCache(assetPath, (bundleCache) =>
            {
                bundleCache?.LoadAsset(assetPath, callback);
            });
        }

        private void LoadBundleCache(string assetPath, Action<AssetBundleCache> callback)
        {
            var bundleInfo = PackInfo_.GetBundleInfoFromAssetPath(assetPath);
            if (bundleInfo == null)
            {
                LLog.Error($"can't get bundle info : {assetPath}");
                callback?.Invoke(null);
                return;
            }
            
            LoadBundleCache(bundleInfo, callback);
        }

        private void LoadBundleCache(BundleInfo info, Action<AssetBundleCache> callback)
        {
            if (BundleCache_.TryGetValue(info.BundlePath, out var bundle))
            {
                callback?.Invoke(bundle);
                return;
            }

            if (BundleCacheLoaderCallback_.TryGetValue(info.BundlePath, out var list))
            {
                list.Add(callback);
                return;
            }

            BundleCacheLoaderCallback_.Add(info.BundlePath, new List<Action<AssetBundleCache>> {callback});
            
            LoadDependencyBundleCache(info, (isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                LoadBundleCacheInternal(info, (loadBundle) =>
                {
                    BundleCache_.Add(info.BundlePath, loadBundle);
                    
                    foreach (var loader in BundleCacheLoaderCallback_[info.BundlePath])
                    {
                        loader?.Invoke(loadBundle);
                    }
                    
                    BundleCacheLoaderCallback_.Remove(info.BundlePath);
                });
            });
        }
        
        private void LoadDependencyBundleCache(BundleInfo info, Action<bool> callback)
        {
            var loadCompletedCount = 0;

            if (info.DependencyList.Length == 0)
            {
                callback?.Invoke(true);
                return;
            }

            foreach (var dependency in info.DependencyList)
            {
                var bundlePath = dependency;
                var dependencyInfo = PackInfo_.GetBundleInfoFromBundlePath(bundlePath);
                if (dependencyInfo == null)
                {
                    LLog.Error($"can't get dependency bundle info : {bundlePath}");
                    callback?.Invoke(false);
                    return;
                }
                
                LoadBundleCache(dependencyInfo, (dependencyBundle) =>
                {
                    if (dependencyBundle == null)
                    {
                        callback?.Invoke(false);
                        return;
                    }
                    
                    loadCompletedCount++;

                    if (loadCompletedCount >= info.DependencyList.Length)
                    {
                        callback?.Invoke(true);
                    }
                });
            }
        }

        private void LoadBundleCacheInternal(BundleInfo info, Action<AssetBundleCache> callback)
        {
            var cache = new AssetBundleCache(info);
            cache.Load((isLoaded) =>
            {
                if (!isLoaded)
                {
                    LLog.Error($"load bundle error : {info.BundlePath}");
                    callback?.Invoke(null);
                    return;
                }
                
                callback?.Invoke(cache);
            });
        }
    }
}