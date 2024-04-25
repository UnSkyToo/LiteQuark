namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        public T LoadAssetSync<T>() where T : UnityEngine.Object
        {
            if (IsLoaded)
            {
                IncRef();
                return Asset as T;
            }

            T asset = default;
            
            if (AssetRequest_ != null)
            {
                asset = AssetRequest_.asset as T;
            }
            else
            {
                var name = PathUtils.GetFileName(AssetPath_);
                BeginLoadTime_ = UnityEngine.Time.realtimeSinceStartupAsDouble;
                asset = Cache.Bundle.LoadAsset<T>(name);
            }

            if (asset != null)
            {
                OnAssetLoaded(asset);
                IncRef();
            }
            
            return asset;
        }
    }
}