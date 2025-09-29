namespace LiteQuark.Runtime
{
    internal partial class AssetBundleCache : ITick, IDispose
    {
        internal BundleVisitorInfo GetVisitorInfo()
        {
            var memSize = IsLoaded ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Bundle) : 0L;
            var info = new BundleVisitorInfo(_bundleInfo.BundlePath, memSize, _refCount, Stage.ToString(), _retainTime);
            
            foreach (var chunk in _assetCacheMap)
            {
                info.AddAssetVisitor(chunk.Value.GetVisitorInfo());
            }
            
            return info;
        }
    }
}