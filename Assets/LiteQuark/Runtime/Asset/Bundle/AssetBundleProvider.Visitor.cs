using System.Linq;

namespace LiteQuark.Runtime
{
    internal sealed partial class AssetBundleProvider : IAssetProvider
    {
        internal VisitorInfo GetVisitorInfo()
        {
            var info = new VisitorInfo("Bundle",
                _bundleCacheMap.Select(chunk => chunk.Value.GetVisitorInfo()).ToArray());
            return info;
        }
    }
}