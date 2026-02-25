namespace LiteQuark.Runtime
{
    internal readonly struct VisitorInfo
    {
        public string Tag { get; }
        public BundleVisitorInfo[] BundleVisitors { get; }

        public VisitorInfo(string tag, BundleVisitorInfo[] bundleVisitors)
        {
            Tag = tag;
            BundleVisitors = bundleVisitors;
        }
    }
    
    internal readonly struct BundleVisitorInfo
    {
        public string BundlePath { get; }
        public long MemorySize { get; }
        public int RefCount { get; }
        public string Stage { get; }
        public float RetainTime { get; }
        public AssetVisitorInfo[] AssetVisitors { get; }

        public BundleVisitorInfo(string bundlePath, long memorySize, int refCount, string stage, float retainTime, AssetVisitorInfo[] assetVisitors)
        {
            BundlePath = bundlePath;
            MemorySize = memorySize;
            RefCount = refCount;
            Stage = stage;
            RetainTime = retainTime;
            AssetVisitors = assetVisitors;
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