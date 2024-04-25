using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        public void LoadAssetAsync<T>(Action<T> callback) where T : UnityEngine.Object
        {
            LoadAssetAsync<T>((bool isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                callback?.Invoke(Asset as T);
            });
        }

        private void LoadAssetAsync<T>(Action<bool> callback) where T : UnityEngine.Object
        {
            if (IsLoaded)
            {
                IncRef();
                callback?.Invoke(true);
                return;
            }
            
            AssetLoaderCallbackList_.Add(callback);
            if (AssetRequest_ != null)
            {
                return;
            }

            var name = PathUtils.GetFileName(AssetPath_);
            AssetRequest_ = Cache.Bundle.LoadAssetAsync<T>(name);
            AssetRequest_.completed += OnAssetRequestLoadCompleted;
        }
        
        private void OnAssetRequestLoadCompleted(UnityEngine.AsyncOperation op)
        {
            op.completed -= OnAssetRequestLoadCompleted;
            
            var asset = (op as UnityEngine.AssetBundleRequest)?.asset;
            if (asset != null)
            {
                OnAssetLoaded(asset);
                
                foreach (var loader in AssetLoaderCallbackList_)
                {
                    IncRef();
                    loader?.Invoke(true);
                }
            }
            else
            {
                LLog.Error($"load asset failed : {AssetPath_}");
                
                foreach (var loader in AssetLoaderCallbackList_)
                {
                    loader?.Invoke(false);
                }
            }
            
            AssetLoaderCallbackList_.Clear();
        }
    }
}