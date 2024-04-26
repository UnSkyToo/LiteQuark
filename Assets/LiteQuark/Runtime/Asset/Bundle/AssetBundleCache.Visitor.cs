namespace LiteQuark.Runtime
{
    internal partial class AssetBundleCache : ITick, IDispose
    {
        internal BundleVisitorInfo GetVisitorInfo()
        {
            var info = new BundleVisitorInfo(Info.BundlePath, RefCount_, Stage.ToString(), RetainTime_);

            foreach (var chunk in AssetCacheMap_)
            {
                info.AddAssetVisitor(chunk.Value.GetVisitorInfo());
            }
            
            return info;
        }
    }
}