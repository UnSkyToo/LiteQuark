using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LiteQuark.Runtime
{
    public sealed class AssetBundleCache
    {
        public bool IsLoad { get; private set; }
        public bool Unused => (RefCount_ <= 0 && IsLoad == true);

        private readonly BundleInfo Info_;
        private AssetBundleCreateRequest Request_;
        private AssetBundle Bundle_;
        private int RefCount_;

        private readonly Dictionary<string, UnityEngine.Object> AssetCache_ = new Dictionary<string, Object>();

        public AssetBundleCache(BundleInfo info)
        {
            Info_ = info;
            Bundle_ = null;
            IsLoad = false;
            RefCount_ = 0;
        }

        public void Load(Action<bool> callback)
        {
            TaskManager.Instance.AddTask(LoadAsync(Info_.BundlePath, callback));
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
                LiteLog.Instance.Error("LiteEngine", $"load asset bundle failed : {path}");
                callback?.Invoke(false);
            }
        }

        public void Unload()
        {
            if (RefCount_ > 0)
            {
                LiteLog.Instance.Warning("LiteEngine", $"unload bundle leak : {Info_.BundlePath}({RefCount_})");
            }

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

        public void LoadAsset<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            if (AssetCache_.TryGetValue(assetPath, out var asset))
            {
                callback?.Invoke(asset as T);
                return;
            }
            
            TaskManager.Instance.AddTask(LoadAssetAsync(assetPath, callback));
        }

        private IEnumerator LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var name = PathHelper.GetFileName(assetPath);
            var request = Bundle_.LoadAssetAsync<T>(name);
            yield return request;
            if (request.isDone)
            {
                AssetCache_.Add(assetPath, request.asset);
                callback?.Invoke(request.asset as T);
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }
}