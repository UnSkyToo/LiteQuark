using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;

namespace LiteQuark.Editor
{
    internal static class ResDependencyChecker
    {
        public static List<List<string>> FindUniqueCycles(VersionPackInfo versionPackInfo)
        {
            var bundleDict = versionPackInfo.BundleList.ToDictionary(b => b.BundlePath);

            var visited = new HashSet<string>();
            var recursionStack = new List<string>();
            var cycles = new List<List<string>>();
            var seen = new HashSet<string>(); // 用于去重

            foreach (var bundle in versionPackInfo.BundleList)
            {
                if (!visited.Contains(bundle.BundlePath))
                {
                    DFS(bundle.BundlePath, bundleDict, visited, recursionStack, cycles, seen);
                }
            }

            return cycles;
        }

        private static void DFS(
            string bundlePath,
            Dictionary<string, BundleInfo> bundleDict,
            HashSet<string> visited,
            List<string> recursionStack,
            List<List<string>> cycles,
            HashSet<string> seen)
        {
            visited.Add(bundlePath);
            recursionStack.Add(bundlePath);

            if (bundleDict.TryGetValue(bundlePath, out var bundle) && bundle.DependencyList != null)
            {
                foreach (var dep in bundle.DependencyList)
                {
                    if (!visited.Contains(dep))
                    {
                        DFS(dep, bundleDict, visited, recursionStack, cycles, seen);
                    }
                    else
                    {
                        var index = recursionStack.IndexOf(dep);
                        if (index != -1)
                        {
                            var cycle = recursionStack.Skip(index).ToList();
                            cycle.Add(dep);

                            var normalized = NormalizeCycle(cycle);
                            var key = string.Join("->", normalized);

                            if (seen.Add(key))
                            {
                                cycles.Add(normalized);
                            }
                        }
                    }
                }
            }

            recursionStack.RemoveAt(recursionStack.Count - 1);
        }

        /// <summary>
        /// 把环路旋转到字典序最小的节点开头，用于去重
        /// </summary>
        private static List<string> NormalizeCycle(List<string> cycle)
        {
            // 去掉最后一个重复节点
            cycle = cycle.Take(cycle.Count - 1).ToList();

            var minIndex = 0;
            for (var i = 1; i < cycle.Count; i++)
            {
                if (string.Compare(cycle[i], cycle[minIndex], StringComparison.Ordinal) < 0)
                {
                    minIndex = i;
                }
            }

            var rotated = cycle.Skip(minIndex).Concat(cycle.Take(minIndex)).ToList();
            rotated.Add(rotated[0]); // 补回闭合节点
            return rotated;
        }
    }
}