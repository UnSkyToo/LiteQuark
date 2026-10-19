namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        internal Lease<T> AcquireLease<T>(T asset) where T : UnityEngine.Object
        {
            return new Lease<T>(this, asset);
        }

        internal sealed class Lease<T> : IDispose where T : UnityEngine.Object
        {
            internal T Asset { get; private set; }

            private AssetInfoCache _cache;

            public Lease(AssetInfoCache cache, T asset)
            {
                _cache = cache;
                Asset = asset;
                cache?.IncRef();
            }

            public void Dispose()
            {
                var cache = _cache;
                _cache = null;
                Asset = null;

                if (cache == null || cache.Stage == AssetCacheStage.Unloaded)
                {
                    return;
                }

                cache.DecRef();
            }

            internal static Lease<T> CreateUncached(T asset)
            {
                return new Lease<T>(null, asset);
            }
        }
    }
}
