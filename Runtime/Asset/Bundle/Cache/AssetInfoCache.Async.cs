using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public void LoadAssetAsync<T>(Action<T> callback) where T : UnityEngine.Object
        {
            InternalLoadAssetAsync<T>((isLoaded) =>
            {
                if (!isLoaded)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                callback?.Invoke(Asset as T);
            });
        }

        private void InternalLoadAssetAsync<T>(Action<bool> callback) where T : UnityEngine.Object
        {
            if (IsLoaded)
            {
                IncRef();
                callback?.Invoke(true);
                return;
            }
            
            IncRef();
            AssetLoaderCallbackList_.Add(callback);
            if (Stage == AssetCacheStage.Loading)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(AssetPath_);

            LoadAssetTask_ = LiteRuntime.Task.LoadAssetTask<T>(Cache_.Bundle, name, HandleAssetLoadCompleted);
        }

        private void HandleAssetLoadCompleted(UnityEngine.Object asset)
        {
            LoadAssetTask_ = null;

            var isLoaded = OnAssetLoaded(asset);
            
            foreach (var loader in AssetLoaderCallbackList_)
            {
                loader?.Invoke(isLoaded);
            }
            
            AssetLoaderCallbackList_.Clear();
        }
    }
}