namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDispose
    {
        internal Lease AcquireLease()
        {
            return new Lease(this);
        }

        internal sealed class Lease : IDispose
        {
            internal UnityEngine.SceneManagement.Scene? Scene  => _cache != null
                ? _cache.Scene
                : throw new System.ObjectDisposedException(nameof(Lease));

            private SceneInfoCache _cache;

            public Lease(SceneInfoCache cache)
            {
                _cache = cache;
            }

            public void Dispose()
            {
                var cache = _cache;
                _cache = null;

                if (cache == null || cache.Stage == AssetCacheStage.Unloaded)
                {
                    return;
                }

                cache.Unload();
            }
        }
    }
}
