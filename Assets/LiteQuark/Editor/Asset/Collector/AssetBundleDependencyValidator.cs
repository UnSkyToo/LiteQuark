using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class AssetBundleDependencyValidator
    {
        internal static void Validate(Dictionary<string, BundleInfo> bundles)
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var recursionStack = new List<string>();
            var seen = new HashSet<string>();

            foreach (var bundlePath in bundles.Keys)
            {
                if (!visited.Contains(bundlePath))
                {
                    FindCycles(bundlePath, bundles, visited, recursionStack, cycles, seen);
                }
            }

            if (cycles.Count > 0)
            {
                var paths = string.Join(Environment.NewLine, cycles.Select(cycle => string.Join(" -> ", cycle)));
                var message = $"发现循环依赖:{Environment.NewLine}{paths}";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }
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
