#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal static class SimulatePackSource
    {
        internal static Func<(VersionPackInfo PackInfo, IReadOnlyDictionary<string, string> AssetSourcePaths)> Collect { get; set; }

        internal static (VersionPackInfo PackInfo, IReadOnlyDictionary<string, string> AssetSourcePaths) CreateSnapshot()
        {
            var collect = Collect ?? throw new InvalidOperationException("Simulate pack source is not registered by LiteQuark.Editor.");
            var snapshot = collect();
            if (snapshot.PackInfo == null || snapshot.AssetSourcePaths == null)
            {
                throw new InvalidOperationException("Simulate pack source returned an incomplete snapshot.");
            }
            return snapshot;
        }
    }
}
#endif
