using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal sealed class AssetBundleCache
    {
        public BundleInfo Info { get; }
        public bool IsLoad { get; private set; }
        public bool Unused => (RefCount_ <= 0 && IsLoad == true);

        private AssetBundleCreateRequest Request_;
        private AssetBundle Bundle_;
        private int RefCount_;

        private readonly List<AssetBundleCache> DependencyCacheList_ = new();
        private readonly Dictionary<string, UnityEngine.Object> AssetCache_ = new();
        private readonly Dictionary<string, List<Action<bool>>> AssetLoaderCallback_ = new();

        public AssetBundleCache(BundleInfo info)
        {
            Info = info;
            Bundle_ = null;
            IsLoad = false;
            RefCount_ = 0;
        }

        public string[] GetDependencyPathList()
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

        public void Load(Action<bool> callback)
        {
            if (IsLoad)
            {
                callback?.Invoke(true);
                return;
            }
            
            LiteRuntime.Get<TaskSystem>().AddTask(LoadAsync(Info.BundlePath, callback));
        }

        private IEnumerator LoadAsync(string path, Action<bool> callback)
        {
            IsLoad = false;
            var fullPath = PathUtils.GetFullPathInRuntime(path);
            Request_ = AssetBundle.LoadFromFileAsync(fullPath);
            
            yield return Request_;
            
            if (Request_.isDone)
            {
                Bundle_ = Request_.assetBundle;
                RefCount_ = 0;
                IsLoad = true;
                Request_ = null;
                callback?.Invoke(true);
            }
            else
            {
                LLog.Error($"load asset bundle failed : {path}");
                callback?.Invoke(false);
            }
        }

        public void Unload()
        {
            if (RefCount_ > 0)
            {
                LLog.Warning($"unload bundle leak : {Info.BundlePath}({RefCount_})");
            }

            foreach (var cache in DependencyCacheList_)
            {
                cache.DecRef();

                if (cache.Unused)
                {
                    cache.Unload();
                }
            }
            DependencyCacheList_.Clear();

            foreach (var asset in AssetCache_)
            {
                if (asset.Value is not GameObject)
                {
                    Resources.UnloadAsset(asset.Value);
                }
            }
            AssetCache_.Clear();
            
            AssetLoaderCallback_.Clear();
            
            if (Bundle_ != null)
            {
                Bundle_.Unload(true);
                Bundle_ = null;
            }

            IsLoad = false;
            RefCount_ = 0;
        }

        public void LoadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            // TODO same path, but type different, like (xxx Texture2D & xxx Sprite)
            if (AssetCache_.TryGetValue(assetPath, out var asset))
            {
                callback?.Invoke(CreateAsset<T>(assetPath));
                return;
            }

            if (AssetLoaderCallback_.TryGetValue(assetPath, out var list))
            {
                list.Add(callback);
                return;
            }

            AssetLoaderCallback_.Add(assetPath, new List<Action<bool>> { callback });
            LiteRuntime.Get<TaskSystem>().AddTask(LoadAssetAsync<T>(assetPath));
        }

        private IEnumerator LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            var name = PathUtils.GetFileName(assetPath);
            var request = Bundle_.LoadAssetAsync<T>(name);
            yield return request;
            if (request.isDone)
            {
                AssetCache_.Add(assetPath, request.asset);
            }
            
            foreach (var loader in AssetLoaderCallback_[assetPath])
            {
                loader?.Invoke(request.isDone);
            }
            AssetLoaderCallback_.Remove(assetPath);
        }

        public void UnloadAsset(string assetPath)
        {
            DecRef();
        }

        public T CreateAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            if (AssetCache_.TryGetValue(assetPath, out var asset))
            {
                IncRef();
                return asset as T;
            }

            return null;
        }
    }
}