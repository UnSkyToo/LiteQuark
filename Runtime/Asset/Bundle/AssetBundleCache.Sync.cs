using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : IDisposable
    {
        private bool ForceLoadBundleComplete()
        {
            if (IsLoaded)
            {
                return true;
            }

            foreach (var bundle in DependencyCacheList_)
            {
                if (!bundle.ForceLoadBundleComplete())
                {
                    return false;
                }
            }

            if (BundleRequest_ != null)
            {
                var bundle = BundleRequest_.assetBundle;
                return bundle != null;
            }

            return true;
        }
        
        public bool LoadBundleSync()
        {
            if (IsLoaded)
            {
                return true;
            }

            if (BundleRequest_ != null)
            {
                return ForceLoadBundleComplete();
            }
            
            var fullPath = PathUtils.GetFullPathInRuntime(Info.BundlePath);
            var bundle = UnityEngine.AssetBundle.LoadFromFile(fullPath);

            if (bundle != null)
            {
                OnBundleLoaded(bundle);
                return true;
            }
            else
            {
                LLog.Error($"load asset bundle failed : {Info.BundlePath}");
                return false;
            }
        }

        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            if (AssetExisted(assetPath))
            {
                return AssetCacheMap_[assetPath] as T;
            }

            T asset = null;

            if (AssetRequestMap_.TryGetValue(assetPath, out var request))
            {
                return request.asset as T;
            }
            
            var name = PathUtils.GetFileName(assetPath);
            asset = Bundle_.LoadAsset<T>(name);
            
            if (asset != null)
            {
                AssetCacheMap_.Add(assetPath, asset);
            }
            
            return asset;
        }
    }
}