﻿using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
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
            if (Stage == AssetCacheStage.Loaded)
            {
                IncRef();
                callback?.Invoke(true);
                return;
            }
            
            AssetLoaderCallbackList_.Add(callback);
            if (Stage == AssetCacheStage.Loading)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(AssetPath_);
            AssetRequest_ = Cache.GetBundle().LoadAssetAsync<T>(name);
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
                Stage = AssetCacheStage.Invalid;
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