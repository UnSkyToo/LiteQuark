using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class BundleInfo
    {
        public int BundleID { get; set; }
        public string BundlePath { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
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
            BundlePath = AssetPath.NormalizeBundlePath(bundlePath);
            AssetList = assetList;
            DependencyList = dependencyList ?? Array.Empty<string>();
        }

        internal BundleInfo WithEncodedDependencies(Dictionary<string, string> bundleIds)
        {
            if (DependencyList.Length == 0)
            {
                return this;
            }

            var copy = (BundleInfo)MemberwiseClone();
            copy.DependencyList = new string[DependencyList.Length];
            for (var i = 0; i < DependencyList.Length; i++)
            {
                var path = DependencyList[i];
                if (!bundleIds.TryGetValue(path, out var id))
                {
                    throw new KeyNotFoundException($"Dependency not found: {path}, bundle: {BundlePath}");
                }
                copy.DependencyList[i] = id;
            }
            return copy;
        }

        public override string ToString()
        {
            return $"{BundleID}:{BundlePath}_{Hash}";
        }
    }
}
