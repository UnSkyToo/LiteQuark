namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        internal AssetVisitorInfo GetVisitorInfo()
        {
            var memSize = IsLoaded ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Asset) : 0L;
            var info = new AssetVisitorInfo(_assetPath, memSize, _refCount, Stage.ToString(), _retainTime);
            return info;
        }
    }
}