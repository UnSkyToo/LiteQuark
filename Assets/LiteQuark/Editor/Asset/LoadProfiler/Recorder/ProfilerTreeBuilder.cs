using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// 依赖树构建器
    /// 从 Session 的 Records 构建依赖关系树
    /// </summary>
    internal static class ProfilerTreeBuilder
    {
        /// <summary>
        /// 为 Session 构建依赖树
        /// </summary>
        public static void Build(ProfilerSession session)
        {
            if (session.DependencyTree == null || session.Records.Count == 0) return;

            var nodeMap = new Dictionary<string, ProfilerDependencyNode>();
            var root = session.DependencyTree;
            nodeMap[root.Path] = root;

            // 第一遍：创建所有 Bundle 节点
            foreach (var record in session.Records)
            {
                if (record.Type == AssetLoadEventType.Bundle)
                {
                    var node = new ProfilerDependencyNode(record);
                    nodeMap[record.BundlePath] = node;
                }
            }

            // 第二遍：建立 Bundle 之间的依赖关系
            foreach (var record in session.Records)
            {
                if (record.Type != AssetLoadEventType.Bundle) continue;
                if (!nodeMap.TryGetValue(record.BundlePath, out var node)) continue;

                foreach (var dep in record.Dependencies)
                {
                    if (!nodeMap.TryGetValue(dep, out var depNode))
                    {
                        // 依赖不在 Records 中（可能是缓存的），创建虚拟节点
                        depNode = ProfilerDependencyNode.CreateVirtual(dep, AssetLoadEventType.Bundle, AssetLoadEventSource.Cached);
                        nodeMap[dep] = depNode;
                    }
                    
                    if (depNode.Parent != null) continue;

                    node.AddChild(depNode);
                }
            }

            // 第三遍：把没有父节点的 Bundle 挂载到根节点，并处理 Asset/Scene
            foreach (var record in session.Records)
            {
                if (record.Type == AssetLoadEventType.Bundle)
                {
                    if (nodeMap.TryGetValue(record.BundlePath, out var node) && node.Parent == null)
                    {
                        root.AddChild(node);
                    }
                }
                else if (record.Type == AssetLoadEventType.Asset || record.Type == AssetLoadEventType.Scene)
                {
                    var assetNode = new ProfilerDependencyNode(record);
                    if (nodeMap.TryGetValue(record.BundlePath, out var bundleNode))
                    {
                        bundleNode.AddChild(assetNode);
                    }
                    else
                    {
                        root.AddChild(assetNode);
                    }
                }
            }
        }
    }
}
