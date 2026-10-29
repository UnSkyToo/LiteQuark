namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDisposeAsync
    {
        internal Lease<T> AcquireLease<T>() where T : UnityEngine.Object
        {
            return new Lease<T>(this);
        }

        internal sealed class Lease<T> : IAssetLease<T> where T : UnityEngine.Object
        {
            public T Value { get; private set; }

            private AssetInfoCache _cache;
            private bool _isDisposed;

            public Lease(AssetInfoCache cache)
            {
                _cache = cache;
                _isDisposed = false;
                Value = null;
                cache?.IncRef();
            }

            public void Dispose()
            {
                var cache = _cache;
                _cache = null;
                Value = null;
                _isDisposed = true;

                if (cache == null || (cache.Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded))
                {
                    return;
                }

                cache.DecRef();
            }

            internal bool TryBindAsset(UnityEngine.Object asset)
            {
                if (_isDisposed || asset == null)
                {
                    return false;
                }

                if (asset is not T assetT)
                {
                    LLog.Error("Asset type mismatch : expected {0}, actual {1}", typeof(T).Name, asset.GetType().Name);
                    return false;
                }

                Value = assetT;
                return true;
            }

            internal static Lease<T> CreateUncached(T asset)
            {
                return new Lease<T>(null) { Value = asset };
            }
        }
    }
}
