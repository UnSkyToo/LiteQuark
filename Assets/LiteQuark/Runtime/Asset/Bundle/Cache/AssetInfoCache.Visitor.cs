namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        internal AssetVisitorInfo GetVisitorInfo()
        {
            var info = new AssetVisitorInfo(_assetPath, _refCount, Stage.ToString(), _retainTime);
            return info;
        }
    }
}