namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        internal AssetVisitorInfo GetVisitorInfo()
        {
            var memSize = IsLoaded ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(Asset) : 0L;
            var info = new AssetVisitorInfo(_assetPath, memSize, _refCount, Stage.ToString());
            return info;
        }
    }
}
