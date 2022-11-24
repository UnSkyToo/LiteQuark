using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime.Internal
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

        public void Load(Action<bool> callback)
        {
            LiteRuntime.GetTaskSystem().AddTask(LoadAsync(Info.BundlePath, callback));
        }

        private IEnumerator LoadAsync(string path, Action<bool> callback)
        {
            if (IsLoad)
            {
                callback?.Invoke(true);
                yield break;
            }

            IsLoad = false;
            var fullPath = PathHelper.GetFullPathInRuntime(path);
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
            
            if (Bundle_ != null)
            {
                Bundle_.Unload(true);
                Bundle_ = null;
            }

            IsLoad = false;
            RefCount_ = 0;
        }

        public void IncRef()
        {
            RefCount_++;
        }

        public void DecRef()
        {
            RefCount_--;
        }

        public void AddDependencyCache(AssetBundleCache cache)
        {
            cache.IncRef();
            DependencyCacheList_.Add(cache);
        }

        public void LoadAsset<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            if (AssetCache_.TryGetValue(assetPath, out var asset))
            {
                IncRef();
                callback?.Invoke(asset as T);
                return;
            }

            LiteRuntime.GetTaskSystem().AddTask(LoadAssetAsync(assetPath, callback));
        }

        private IEnumerator LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var name = PathHelper.GetFileName(assetPath);
            var request = Bundle_.LoadAssetAsync<T>(name);
            yield return request;
            if (request.isDone)
            {
                AssetCache_.Add(assetPath, request.asset);
                IncRef();
                callback?.Invoke(request.asset as T);
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }
}