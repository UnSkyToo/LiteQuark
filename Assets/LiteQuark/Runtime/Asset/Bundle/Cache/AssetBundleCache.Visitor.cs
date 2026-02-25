using System.Linq;

namespace LiteQuark.Runtime
{
    internal partial class AssetBundleCache : ITick, IDispose
    {
        internal BundleVisitorInfo GetVisitorInfo()
        {
            var memSize = IsLoaded ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Bundle) : 0L;
            var info = new BundleVisitorInfo(_bundleInfo.BundlePath,
                memSize,
                _refCount,
                Stage.ToString(),
                _retainTime,
                _assetCacheMap.Select(chunk => chunk.Value.GetVisitorInfo()).ToArray());
            return info;
        }
    }
}