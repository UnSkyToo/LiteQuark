using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleCache : ITick, IDisposeAsync
    {
        internal BundleVisitorInfo GetVisitorInfo()
        {
            var memSize = IsLoaded ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Bundle) : 0L;
            var info = new BundleVisitorInfo(_bundleInfo.BundlePath,
                memSize,
                _refCount,
                Stage.ToString(),
                _retainTime,
                _sceneCacheMap.Select(chunk => chunk.GetVisitorInfo()).ToArray(),
                _assetCacheMap.Select(chunk => chunk.Value.GetVisitorInfo()).ToArray());
            return info;
        }
    }
}
