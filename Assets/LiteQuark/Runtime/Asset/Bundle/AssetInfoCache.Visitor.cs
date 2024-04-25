namespace LiteQuark.Runtime
{
    internal sealed partial class AssetInfoCache : IDispose
    {
        internal AssetVisitorInfo GetVisitorInfo()
        {
            var info = new AssetVisitorInfo(AssetPath_, RefCount_, IsLoaded);
            return info;
        }
    }
}