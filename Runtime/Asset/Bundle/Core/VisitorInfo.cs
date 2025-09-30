using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal readonly struct VisitorInfo
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
    
    internal readonly struct BundleVisitorInfo
    {
        public string BundlePath { get; }
        public long MemorySize { get; }
        public int RefCount { get; }
        public string Stage { get; }
        public float RetainTime { get; }
        
        public List<AssetVisitorInfo> AssetVisitorList { get; }

        public BundleVisitorInfo(string bundlePath, long memorySize, int refCount, string stage, float retainTime)
        {
            BundlePath = bundlePath;
            MemorySize = memorySize;
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
    
    internal readonly struct AssetVisitorInfo
    {
        public string AssetPath { get; }
        public long MemorySize { get; }
        public int RefCount { get; }
        public string Stage { get; }
        public float RetainTime { get; }

        public AssetVisitorInfo(string assetPath, long memorySize, int refCount, string stage, float retainTime)
        {
            AssetPath = assetPath;
            MemorySize = memorySize;
            RefCount = refCount;
            Stage = stage;
            RetainTime = retainTime;
        }
    }
}