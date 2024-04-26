using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public readonly struct VisitorInfo
    {
        public string Tag { get; }
        public List<BundleVisitorInfo> BundleVisitorList { get; }

        public VisitorInfo(string  tag)
        {
            Tag = tag;
            BundleVisitorList = new List<BundleVisitorInfo>();
        }
        
        public void AddBundleVisitor(BundleVisitorInfo info)
        {
            BundleVisitorList.Add(info);
        }
    }
    
    public readonly struct BundleVisitorInfo
    {
        public string BundlePath { get; }
        public int RefCount { get; }
        public string Stage { get; }
        public float RetainTime { get; }
        
        public List<AssetVisitorInfo> AssetVisitorList { get; }

        public BundleVisitorInfo(string bundlePath, int refCount, string stage, float retainTime)
        {
            BundlePath = bundlePath;
            RefCount = refCount;
            Stage = stage;
            RetainTime = retainTime;

            AssetVisitorList = new List<AssetVisitorInfo>();
        }

        public void AddAssetVisitor(AssetVisitorInfo info)
        {
            AssetVisitorList.Add(info);
        }
    }
    
    public readonly struct AssetVisitorInfo
    {
        public string AssetPath { get; }
        public int RefCount { get; }
        public string Stage { get; }
        public float RetainTime { get; }

        public AssetVisitorInfo(string assetPath, int refCount, string stage, float retainTime)
        {
            AssetPath = assetPath;
            RefCount = refCount;
            Stage = stage;
            RetainTime = retainTime;
        }
    }
}