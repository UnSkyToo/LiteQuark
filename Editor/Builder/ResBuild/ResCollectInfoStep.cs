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
            var bundlePack = builder.Collector.GetBundlePackInfo(builder);
            ApplyBundleInfo(bundlePack);
        }

        private void ApplyBundleInfo(BundlePackInfo packInfo)
        {
            foreach (var buildInfo in packInfo.BundleList)
            {
                if (buildInfo.DependencyList.Contains(buildInfo.BundlePath))
                {
                    LEditorLog.Error($"loop reference : {buildInfo.BundlePath}");
                    continue;
                }
                
                foreach (var assetPath in buildInfo.AssetList)
                {
                    var fullPath = assetPath.StartsWith("assets") ? assetPath : PathUtils.GetFullPathInAssetRoot(assetPath);
                    var importer = AssetImporter.GetAtPath(fullPath);
                    importer.SetAssetBundleNameAndVariant(buildInfo.BundlePath, string.Empty);
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
}