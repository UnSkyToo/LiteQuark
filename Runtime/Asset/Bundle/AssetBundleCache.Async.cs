using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : IDisposable
    {
        public void LoadBundleAsync(Action<bool> callback)
        {
            if (IsLoaded)
            {
                callback?.Invoke(true);
                return;
            }

            BundleLoaderCallback_ = callback;
            var fullPath = PathUtils.GetFullPathInRuntime(Info.BundlePath);
            BundleRequest_ = UnityEngine.AssetBundle.LoadFromFileAsync(fullPath);
            BundleRequest_.completed += OnBundleRequestCompleted;
        }

        private void OnBundleRequestCompleted(UnityEngine.AsyncOperation op)
        {
            op.completed -= OnBundleRequestCompleted;

            var bundle = (op as UnityEngine.AssetBundleCreateRequest)?.assetBundle;
            if (bundle != null)
            {
                OnBundleLoaded(bundle);
                BundleLoaderCallback_?.Invoke(true);
            }
            else
            {
                LLog.Error($"load asset bundle failed : {Info.BundlePath}");
                BundleLoaderCallback_?.Invoke(false);
            }
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
            var name = PathUtils.GetFileName(assetPath);
            var request = Bundle_.LoadAssetAsync<T>(name);
            AssetRequestMap_.Add(assetPath, request);
            RequestToAssetPathMap_.Add(request, assetPath);
            request.completed += OnAssetRequestLoadCompleted;
        }

        private void OnAssetRequestLoadCompleted(UnityEngine.AsyncOperation op)
        {
            op.completed -= OnAssetRequestLoadCompleted;

            var assetPath = RequestToAssetPathMap_[op];
            var asset = (op as UnityEngine.AssetBundleRequest)?.asset;
            if (asset != null)
            {
                AssetCacheMap_.Add(assetPath, asset);
            }
            else
            {
                LLog.Error($"load asset failed : {assetPath}");
            }

            AssetRequestMap_.Remove(assetPath);
            RequestToAssetPathMap_.Remove(op);
            
            foreach (var loader in AssetLoaderCallbackList_[assetPath])
            {
                loader?.Invoke(asset != null);
            }
            AssetLoaderCallbackList_.Remove(assetPath);
        }
    }
}