namespace LiteQuark.Runtime
{
    internal partial class AssetBundleLoader : IAssetLoader
    {
        internal VisitorInfo GetVisitorInfo()
        {
            var info = new VisitorInfo("Bundle");

            foreach (var chunk in BundleCacheMap_)
            {
                info.AddBundleVisitor(chunk.Value.GetVisitorInfo());
            }

            foreach (var chunk in BundleLoaderCallbackList_)
            {
                info.AddBundleVisitor(new BundleVisitorInfo(chunk.Key, 0, false, 0));
            }
            
            return info;
        }
    }
}