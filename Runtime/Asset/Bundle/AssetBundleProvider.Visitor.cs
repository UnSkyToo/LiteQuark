namespace LiteQuark.Runtime
{
    internal partial class AssetBundleProvider : IAssetProvider
    {
        internal VisitorInfo GetVisitorInfo()
        {
            var info = new VisitorInfo("Bundle");

            foreach (var chunk in BundleCacheMap_)
            {
                info.AddBundleVisitor(chunk.Value.GetVisitorInfo());
            }
            
            return info;
        }
    }
}