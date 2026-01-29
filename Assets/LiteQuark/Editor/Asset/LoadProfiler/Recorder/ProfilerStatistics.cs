namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// 分析器统计信息
    /// </summary>
    internal struct ProfilerStatistics
    {
        public int TotalRecordCount;
        public float TotalDuration;
        public long TotalSize;

        public int BundleCount;
        public int LocalBundleCount;
        public int RemoteBundleCount;
        public int CachedBundleCount;
        public float BundleTotalDuration;
        public float AverageBundleDuration;

        public int AssetCount;
        public float AssetTotalDuration;
        public float AverageAssetDuration;

        public int SceneCount;
        public float SceneTotalDuration;
        public float AverageSceneDuration;

        public int FailedCount;
    }
}
