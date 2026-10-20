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
        public SceneVisitorInfo[] SceneVisitors { get; }
        public AssetVisitorInfo[] AssetVisitors { get; }

        public BundleVisitorInfo(string bundlePath, long memorySize, int refCount, string stage, float retainTime, SceneVisitorInfo[] sceneVisitors, AssetVisitorInfo[] assetVisitors)
        {
            BundlePath = bundlePath;
            MemorySize = memorySize;
            RefCount = refCount;
            Stage = stage;
            RetainTime = retainTime;
            SceneVisitors = sceneVisitors;
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

    internal readonly struct SceneVisitorInfo
    {
        public string SceneName { get; }

        public SceneVisitorInfo(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
