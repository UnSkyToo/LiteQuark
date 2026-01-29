using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// 资源加载会话，记录一次完整的资源加载请求
    /// </summary>
    internal sealed class ProfilerSession
    {
        public long SessionId { get; internal set; }
        public string RequestPath { get; internal set; }
        public string MainBundlePath { get; internal set; }
        public long RequestTimestamp { get; internal set; }
        public long CompleteTimestamp { get; internal set; }
        public float TotalDuration => CompleteTimestamp > 0 ? CompleteTimestamp - RequestTimestamp : 0;
        public bool IsCompleted => CompleteTimestamp > 0;
        public bool IsSuccess { get; internal set; }
        public List<ProfilerRecord> Records { get; }
        public ProfilerDependencyNode DependencyTree { get; internal set; }
        public int BundleCount { get; internal set; }
        public int AssetCount { get; internal set; }
        public long TotalSize { get; internal set; }

        internal HashSet<string> RequiredBundles { get; }

        public ProfilerSession()
        {
            Records = new List<ProfilerRecord>();
            RequiredBundles = new HashSet<string>();
            Reset();
        }

        public void Reset()
        {
            SessionId = 0;
            RequestPath = string.Empty;
            MainBundlePath = string.Empty;
            RequestTimestamp = 0;
            CompleteTimestamp = 0;
            IsSuccess = false;
            Records.Clear();
            RequiredBundles.Clear();
            DependencyTree = null;
            BundleCount = 0;
            AssetCount = 0;
            TotalSize = 0;
        }

        /// <summary>
        /// 注册一个 Bundle 及其依赖到此 Session
        /// </summary>
        internal void RegisterBundle(string bundlePath, string[] dependencies)
        {
            RequiredBundles.Add(bundlePath);
            if (dependencies != null)
            {
                foreach (var dep in dependencies)
                {
                    RequiredBundles.Add(dep);
                }
            }
        }

        /// <summary>
        /// 检查此 Session 是否需要指定的 Bundle
        /// </summary>
        internal bool RequiresBundle(string bundlePath)
        {
            return RequiredBundles.Contains(bundlePath);
        }

        public void AddRecord(ProfilerRecord record)
        {
            // 只在SessionId未设置时赋值（Bundle可能被多个Session共享）
            if (record.SessionId == 0)
            {
                record.SessionId = SessionId;
            }
            Records.Add(record);

            if (record.Type == AssetLoadEventType.Bundle)
            {
                BundleCount++;
            }
            else if (record.Type == AssetLoadEventType.Asset)
            {
                AssetCount++;
            }

            TotalSize += record.Size;
        }

        public void Complete(bool success, long timestamp)
        {
            CompleteTimestamp = timestamp;
            IsSuccess = success;
        }

        public override string ToString()
        {
            return $"Session[{SessionId}] {RequestPath} ({TotalDuration:F1}ms, Bundles:{BundleCount}, Assets:{AssetCount})";
        }
    }
}
