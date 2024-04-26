namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        public T LoadAssetSync<T>() where T : UnityEngine.Object
        {
            if (Stage == AssetCacheStage.Loaded)
            {
                IncRef();
                return Asset as T;
            }

            T asset = default;
            
            if (Stage == AssetCacheStage.Loading)
            {
                asset = AssetRequest_.asset as T;
                OnAssetLoaded(asset);
            }
            else
            {
                Stage = AssetCacheStage.Loading;
                var name = PathUtils.GetFileName(AssetPath_);
                asset = Cache.GetBundle().LoadAsset<T>(name);
            }

            if (asset != null)
            {
                OnAssetLoaded(asset);
                IncRef();
            }
            else
            {
                Stage = AssetCacheStage.Invalid;
                LLog.Error($"load asset failed : {AssetPath_}");
            }
            
            return asset;
        }
    }
}