using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// 依赖关系节点
    /// </summary>
    internal sealed class ProfilerDependencyNode
    {
        /// <summary>
        /// 关联的加载记录（虚拟节点为 null，用于访问 Dependencies 等额外信息）
        /// </summary>
        public ProfilerRecord Record { get; private set; }

        public string Path { get; private set; }
        public AssetLoadEventType Type { get; private set; }
        public AssetLoadEventSource Source { get; private set; }
        public float Duration { get; private set; }
        public long Size { get; private set; }
        public bool IsSuccess { get; private set; }
        public long RecordId { get; private set; }

        public List<ProfilerDependencyNode> Children { get; }
        public ProfilerDependencyNode Parent { get; set; }

        private ProfilerDependencyNode()
        {
            Children = new List<ProfilerDependencyNode>();
        }

        /// <summary>
        /// 创建根节点或虚拟节点
        /// </summary>
        public ProfilerDependencyNode(string path, AssetLoadEventType type) : this()
        {
            Path = path;
            Type = type;
            Source = AssetLoadEventSource.Unknown;
            IsSuccess = false;
        }

        /// <summary>
        /// 从 Record 创建节点（复制数据到本地字段）
        /// </summary>
        public ProfilerDependencyNode(ProfilerRecord record) : this()
        {
            Record = record;
            Path = record.DisplayPath;
            Type = record.Type;
            Source = record.Source;
            Duration = record.Duration;
            Size = record.Size;
            IsSuccess = record.IsSuccess;
            RecordId = record.RecordId;
        }

        /// <summary>
        /// 创建虚拟节点（用于缓存的依赖）
        /// </summary>
        public static ProfilerDependencyNode CreateVirtual(string path, AssetLoadEventType type, AssetLoadEventSource source, bool isSuccess = true)
        {
            return new ProfilerDependencyNode(path, type)
            {
                Source = source,
                IsSuccess = isSuccess
            };
        }

        public void AddChild(ProfilerDependencyNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public int GetDepth()
        {
            var depth = 0;
            var node = Parent;
            while (node != null)
            {
                depth++;
                node = node.Parent;
            }
            return depth;
        }

        public float GetTotalDuration()
        {
            var total = Duration;
            foreach (var child in Children)
            {
                total += child.GetTotalDuration();
            }
            return total;
        }

        public override string ToString()
        {
            var indent = new string(' ', GetDepth() * 2);
            return $"{indent}[{Type}] {Path} ({Duration:F1}ms, {Source})";
        }
    }
}
