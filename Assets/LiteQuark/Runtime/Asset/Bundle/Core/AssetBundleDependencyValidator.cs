using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteQuark.Runtime
{
    internal static class AssetBundleDependencyValidator
    {
        public static List<List<string>> FindUniqueCycles(VersionPackInfo versionPackInfo)
        {
            var cycles = new List<List<string>>();
            if (versionPackInfo?.BundleList == null || versionPackInfo.BundleList.Length == 0)
            {
                return cycles;
            }

            var bundleDict = versionPackInfo.BundleList.ToDictionary(static b => b.BundlePath);
            var visited = new HashSet<string>();
            var recursionStack = new List<string>();
            var seen = new HashSet<string>();

            foreach (var bundle in versionPackInfo.BundleList)
            {
                if (!visited.Contains(bundle.BundlePath))
                {
                    FindCycles(bundle.BundlePath, bundleDict, visited, recursionStack, cycles, seen);
                }
            }

            return cycles;
        }

        public static string FormatCycles(IEnumerable<List<string>> cycles)
        {
            var sb = new StringBuilder();
            foreach (var cycle in cycles)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.Append(string.Join(" -> ", cycle));
            }

            return sb.ToString();
        }

        private static void FindCycles(
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
                foreach (var dependency in bundle.DependencyList)
                {
                    if (!visited.Contains(dependency))
                    {
                        FindCycles(dependency, bundleDict, visited, recursionStack, cycles, seen);
                        continue;
                    }

                    var index = recursionStack.IndexOf(dependency);
                    if (index < 0)
                    {
                        continue;
                    }

                    var cycle = recursionStack.Skip(index).ToList();
                    cycle.Add(dependency);
                    var normalized = NormalizeCycle(cycle);
                    var key = string.Join("->", normalized);
                    if (seen.Add(key))
                    {
                        cycles.Add(normalized);
                    }
                }
            }

            recursionStack.RemoveAt(recursionStack.Count - 1);
        }

        private static List<string> NormalizeCycle(List<string> cycle)
        {
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
            rotated.Add(rotated[0]);
            return rotated;
        }
    }
}
