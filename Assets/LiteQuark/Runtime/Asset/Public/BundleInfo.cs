using System;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class BundleInfo
    {
        public string BundlePath { get; set; }
        public string[] AssetList { get; set; }
        public string[] DependencyList { get; set; }

        public BundleInfo()
        {
        }

        public BundleInfo(string bundlePath, string[] assetList, string[] dependencyList)
        {
            BundlePath = bundlePath.ToLower();
            AssetList = assetList;
            DependencyList = dependencyList;
        }

        public override string ToString()
        {
            return BundlePath;
        }
    }
}