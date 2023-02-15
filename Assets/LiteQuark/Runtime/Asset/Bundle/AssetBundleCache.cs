using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed class AssetBundleCache : IDisposable
    {
        public BundleInfo Info { get; }
        public bool IsLoaded { get; private set; }
        private bool IsUnloaded_;
        
        public bool IsUsed => RefCount_ > 0;
        
        private readonly List<AssetBundleCache> DependencyCacheList_ = new();
        private readonly Dictionary<string, UnityEngine.Object> AssetCacheMap_ = new();
        private readonly Dictionary<string, List<Action<bool>>> AssetLoaderCallbackList_ = new();
        
        private UnityEngine.AssetBundleCreateRequest Request_;
        private UnityEngine.AssetBundle Bundle_;
        private int RefCount_;
        
        public AssetBundleCache(BundleInfo info)
        {
            Info = info;
            IsLoaded = false;
            IsUnloaded_ = false;

            Request_ = null;
            Bundle_ = null;
            RefCount_ = 0;
        }

        public void Dispose()
        {
            Unload();
            AssetLoaderCallbackList_.Clear();
            DependencyCacheList_.Clear();
            AssetCacheMap_.Clear();

            if (Bundle_ != null)
            {
                Bundle_.Unload(true);
                Bundle_ = null;
            }

            Request_ = null;
            IsLoaded = false;
        }
        
        public void Unload()
        {
            if (IsUnloaded_)
            {
                return;
            }
            
            if (RefCount_ > 0)
            {
                LLog.Warning($"unload bundle leak : {Info.BundlePath}({RefCount_})");
            }
            
            foreach (var cache in DependencyCacheList_)
            {
                cache.DecRef();

                if (!cache.IsUsed)
                {
                    cache.Unload();
                }
            }

            RefCount_ = 0;
            IsUnloaded_ = true;
        }

        public string[] GetAllDependencies()
        {
            return Info.DependencyList;
        }
        
        public void AddDependencyCache(AssetBundleCache cache)
        {
            cache.IncRef();
            DependencyCacheList_.Add(cache);
        }

        public void IncRef()
        {
            RefCount_++;
        }

        public void DecRef()
        {
            RefCount_--;
        }

        public void LoadBundleAsync(Action<bool> callback)
        {
            if (IsLoaded)
            {
                callback?.Invoke(true);
                return;
            }
            
            LiteRuntime.Get<TaskSystem>().AddTask(LoadBundleAsyncTask(Info.BundlePath, callback));
        }
        
        private IEnumerator LoadBundleAsyncTask(string path, Action<bool> callback)
        {
            IsLoaded = false;
            var fullPath = PathUtils.GetFullPathInRuntime(path);
            Request_ = UnityEngine.AssetBundle.LoadFromFileAsync(fullPath);
            
            yield return Request_;
            
            if (Request_.isDone)
            {
                Bundle_ = Request_.assetBundle;
                RefCount_ = 0;
                IsLoaded = true;
                IsUnloaded_ = false;
                Request_ = null;
                callback?.Invoke(true);
            }
            else
            {
                LLog.Error($"load asset bundle failed : {path}");
                callback?.Invoke(false);
            }
        }

        private bool AssetExisted(string assetPath)
        {
            return AssetCacheMap_.ContainsKey(assetPath);
        }

        public UnityEngine.Object[] GetLoadAssetList()
        {
            return AssetCacheMap_.Count == 0 ? Array.Empty<UnityEngine.Object>() : AssetCacheMap_.Values.ToArray();
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            LoadAssetAsync<T>(assetPath, (bool isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                callback?.Invoke(AssetCacheMap_[assetPath] as T);
            });
        }

        private void LoadAssetAsync<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            if (AssetExisted(assetPath))
            {
                callback?.Invoke(true);
                return;
            }
            
            if (AssetLoaderCallbackList_.TryGetValue(assetPath, out var list))
            {
                list.Add(callback);
                return;
            }

            AssetLoaderCallbackList_.Add(assetPath, new List<Action<bool>> { callback });
            LiteRuntime.Get<TaskSystem>().AddTask(LoadAssetAsyncTask<T>(assetPath));
        }
        
        private IEnumerator LoadAssetAsyncTask<T>(string assetPath) where T : UnityEngine.Object
        {
            var name = PathUtils.GetFileName(assetPath);
            var request = Bundle_.LoadAssetAsync<T>(name);
            yield return request;
            if (request.isDone)
            {
                AssetCacheMap_.Add(assetPath, request.asset);
            }
            
            foreach (var loader in AssetLoaderCallbackList_[assetPath])
            {
                loader?.Invoke(request.isDone);
            }
            AssetLoaderCallbackList_.Remove(assetPath);
        }
    }
}