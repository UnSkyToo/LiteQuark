namespace LiteQuark.Runtime
{
    internal partial class AssetBundleProvider : IAssetProvider
    {
        internal VisitorInfo GetVisitorInfo()
        {
            var info = new VisitorInfo("Bundle");

            foreach (var chunk in _bundleCacheMap)
            {
                info.AddBundleVisitor(chunk.Value.GetVisitorInfo());
            }
            
            return info;
        }
    }
}