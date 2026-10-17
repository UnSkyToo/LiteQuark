using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDispose
    {
        private readonly List<Lease> _dependencyLeases = new();

        internal Lease AcquireLease()
        {
            return new Lease(this);
        }

        private void ReleaseDependencyLeases()
        {
            foreach (var lease in _dependencyLeases)
            {
                lease.Dispose();
            }

            _dependencyLeases.Clear();
        }

        internal sealed class Lease : IDispose
        {
            public string BundlePath { get; }

            internal UnityEngine.AssetBundle Bundle => _cache != null
                ? _cache.Bundle
                : throw new System.ObjectDisposedException(nameof(Lease));

            private AssetBundleCache _cache;

            public Lease(AssetBundleCache cache)
            {
                _cache = cache;
                BundlePath = cache.BundlePath;
                cache.IncRef();
            }

            public void Dispose()
            {
                var cache = _cache;
                _cache = null;

                if (cache == null || cache.Stage == AssetCacheStage.Unloaded)
                {
                    return;
                }

                cache.DecRef();
            }
        }
    }
}
