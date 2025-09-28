using System;
using System.Linq;
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
            builder.Collector.Builds = new AssetBundleBuild[versionPack.BundleList.Length];

            for (var i = 0; i < versionPack.BundleList.Length; i++)
            {
                var bundleInfo = versionPack.BundleList[i];
                if (bundleInfo.DependencyList.Contains(bundleInfo.BundlePath))
                {
                    LEditorLog.Error($"loop reference : {bundleInfo.BundlePath}");
                    continue;
                }
                
                var bundlePath = versionPack.GetBundleFileBuildPath(bundleInfo);
                var assetPaths = new string[bundleInfo.AssetList.Length];

                for (var j = 0; j < bundleInfo.AssetList.Length; j++)
                {
                    var assetPath = bundleInfo.AssetList[j];
                    var fullPath = assetPath.StartsWith(LiteConst.AssetRootName, StringComparison.OrdinalIgnoreCase) ? assetPath : PathUtils.GetFullPathInAssetRoot(assetPath);
                    assetPaths[j] = fullPath;
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