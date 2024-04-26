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
        public bool IsLoaded { get; }
        public float LoadTime { get; }
        public float RetainTimeMs { get; }
        
        public List<AssetVisitorInfo> AssetVisitorList { get; }

        public BundleVisitorInfo(string bundlePath, int refCount, bool isLoaded, float loadTime, float retainTimeMs)
        {
            BundlePath = bundlePath;
            RefCount = refCount;
            IsLoaded = isLoaded;
            LoadTime = loadTime;
            RetainTimeMs = retainTimeMs;

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
        public bool IsLoaded { get; }
        public float LoadTime { get; }

        public AssetVisitorInfo(string assetPath, int refCount, bool isLoaded, float loadTime)
        {
            AssetPath = assetPath;
            RefCount = refCount;
            IsLoaded = isLoaded;
            LoadTime = loadTime;
        }
    }
}