namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : ITick, IDispose
    {
        internal AssetVisitorInfo GetVisitorInfo()
        {
            var info = new AssetVisitorInfo(AssetPath_, RefCount_, Stage.ToString(), RetainTime_);
            return info;
        }
    }
}