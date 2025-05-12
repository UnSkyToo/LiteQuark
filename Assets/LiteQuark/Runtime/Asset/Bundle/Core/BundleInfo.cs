using System;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class BundleInfo
    {
        public int BundleID { get; set; }
        public string BundlePath { get; set; }
        public string Hash { get; set; }
        public string[] AssetList { get; set; }
        public string[] DependencyList { get; set; }

        public BundleInfo()
        {
            BundleID = -1;
            BundlePath = string.Empty;
            Hash = string.Empty;
            AssetList = Array.Empty<string>();
            DependencyList = Array.Empty<string>();
        }

        public BundleInfo(int bundleID, string bundlePath, string[] assetList, string[] dependencyList)
        {
            BundleID = bundleID;
            BundlePath = bundlePath.ToLower();
            AssetList = assetList;
            DependencyList = dependencyList ?? Array.Empty<string>();
        }

        public string GetBundlePathWithHash()
        {
            return BundlePath.Replace(LiteConst.BundleFileExt, $"_{Hash}{LiteConst.BundleFileExt}");
        }

        public override string ToString()
        {
            return $"{BundleID}:{BundlePath}_{Hash}";
        }
    }
}