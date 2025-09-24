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
            ApplyVersionPack(versionPack);
        }

        private void ApplyVersionPack(VersionPackInfo packInfo)
        {
            foreach (var buildInfo in packInfo.BundleList)
            {
                if (buildInfo.DependencyList.Contains(buildInfo.BundlePath))
                {
                    LEditorLog.Error($"loop reference : {buildInfo.BundlePath}");
                    continue;
                }

                var bundlePath = packInfo.GetBundleFileBuildPath(buildInfo);
                foreach (var assetPath in buildInfo.AssetList)
                {
                    var fullPath = assetPath.StartsWith("assets") ? assetPath : PathUtils.GetFullPathInAssetRoot(assetPath);
                    var importer = AssetImporter.GetAtPath(fullPath);
                    importer.SetAssetBundleNameAndVariant(bundlePath, string.Empty);
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
}