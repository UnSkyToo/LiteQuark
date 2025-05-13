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
            _assetLoaderCallbackList.Add(callback);
            if (Stage == AssetCacheStage.Loading)
            {
                return;
            }

            Stage = AssetCacheStage.Loading;
            var name = PathUtils.GetFileName(_assetPath);

            _loadAssetTask = LiteRuntime.Task.LoadAssetTask<T>(_cache.Bundle, name, HandleAssetLoadCompleted);
        }

        private void HandleAssetLoadCompleted(UnityEngine.Object asset)
        {
            _loadAssetTask = null;

            var isLoaded = OnAssetLoaded(asset);
            
            foreach (var loader in _assetLoaderCallbackList)
            {
                loader?.Invoke(isLoaded);
            }
            
            _assetLoaderCallbackList.Clear();
        }
    }
}