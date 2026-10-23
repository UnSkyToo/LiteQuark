using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Collect asset bundle info and apply
    /// </summary>
    internal sealed class ResCollectInfoStep : IBuildStep
    {
        public string Name => "Res Collect Info Step";

        public void Execute(ProjectBuilder builder)
        {
            var versionPack = builder.Collector.GetVersionPackInfo(builder);
            CollectAssetBundleBuilds(builder, versionPack);
        }

        private void CollectAssetBundleBuilds(ProjectBuilder builder, VersionPackInfo versionPack)
        {
            var buildPathToBundle = new Dictionary<string, string>(StringComparer.Ordinal);

            builder.Collector.Builds = new AssetBundleBuild[versionPack.BundleList.Length];

            for (var i = 0; i < versionPack.BundleList.Length; i++)
            {
                var bundleInfo = versionPack.BundleList[i];
                var bundlePath = versionPack.GetBundleFileBuildPath(bundleInfo);

                if (buildPathToBundle.TryGetValue(bundlePath, out var existingBundle))
                {
                    throw new InvalidOperationException($"Bundle build path collision: {existingBundle} <-> {bundleInfo.BundlePath}, output: {bundlePath}");
                }

                buildPathToBundle.Add(bundlePath, bundleInfo.BundlePath);

                var assetPaths = new string[bundleInfo.AssetList.Length];
                for (var j = 0; j < bundleInfo.AssetList.Length; j++)
                {
                    assetPaths[j] = builder.Collector.GetAssetSourcePath(bundleInfo.AssetList[j]);
                }

                builder.Collector.Builds[i] = new AssetBundleBuild()
                {
                    assetBundleName = bundlePath,
                    assetNames = assetPaths,
                };
            }
        }
    }
}
