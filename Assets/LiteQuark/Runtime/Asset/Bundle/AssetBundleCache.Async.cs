using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        public void LoadBundleAsync(Action<bool> callback)
        {
            if (Stage == AssetCacheStage.Loaded)
            {
                callback?.Invoke(true);
                return;
            }

            if (Stage == AssetCacheStage.Loading)
            {
                LLog.Error($"repeat bundle loader : {Info.BundlePath}");
                return;
            }

            Stage = AssetCacheStage.Loading;
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
                Stage = AssetCacheStage.Invalid;
                LLog.Error($"load asset bundle failed : {Info.BundlePath}");
                BundleLoaderCallback_?.Invoke(false);
            }
        }
        
        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var cache = GetOrCreateAssetCache(assetPath);
            cache.LoadAssetAsync<T>((asset) =>
            {
                if (asset != null)
                {
                    IncRef();
                }
                
                callback?.Invoke(asset);
            });
        }
    }
}