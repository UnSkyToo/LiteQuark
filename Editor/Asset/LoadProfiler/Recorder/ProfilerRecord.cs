using System;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// 资源加载记录
    /// </summary>
    internal sealed class ProfilerRecord
    {
        public long RecordId { get; internal set; }
        public string AssetPath { get; internal set; }
        public string BundlePath { get; internal set; }
        public AssetLoadEventSource Source { get; internal set; }
        public AssetLoadEventType Type { get; internal set; }
        public long StartTimestamp { get; internal set; }
        public long EndTimestamp { get; internal set; }
        public float Duration => EndTimestamp > 0 ? EndTimestamp - StartTimestamp : 0;
        public long Size { get; internal set; }
        public bool IsSuccess { get; internal set; }
        public string ErrorMessage { get; internal set; }
        public string[] Dependencies { get; internal set; }
        public long SessionId { get; internal set; }

        public ProfilerRecord()
        {
            Reset();
        }

        public void Reset()
        {
            RecordId = 0;
            AssetPath = string.Empty;
            BundlePath = string.Empty;
            Source = AssetLoadEventSource.Unknown;
            Type = AssetLoadEventType.Bundle;
            StartTimestamp = 0;
            EndTimestamp = 0;
            Size = 0;
            IsSuccess = false;
            ErrorMessage = string.Empty;
            Dependencies = Array.Empty<string>();
            SessionId = 0;
        }

        /// <summary>
        /// 获取显示路径（Bundle 类型返回 BundlePath，其他返回 AssetPath）
        /// </summary>
        public string DisplayPath => Type == AssetLoadEventType.Bundle ? BundlePath : AssetPath;

        public override string ToString()
        {
            return $"[{Type}] {DisplayPath} ({Duration:F1}ms, {Source})";
        }
    }
}
