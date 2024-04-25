namespace LiteQuark.Runtime
{
    internal partial class AssetBundleCache : IDispose
    {
        internal BundleVisitorInfo GetVisitorInfo()
        {
            var info = new BundleVisitorInfo(Info.BundlePath, RefCount_, IsLoaded);

            foreach (var chunk in AssetCacheMap_)
            {
                info.AddAssetVisitor(chunk.Value.GetVisitorInfo());
            }
            
            return info;
        }
    }
}