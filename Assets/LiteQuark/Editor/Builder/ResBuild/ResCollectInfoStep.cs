using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Collect asset bundle info and apply, write pack info to json
    /// </summary>
    internal sealed class ResCollectInfoStep : IBuildStep
    {
        public string Name => "Res Collect Info Step";

        private ResCollector Collector_ = new ResCollector();

        public void Execute(ProjectBuilder builder)
        {
            var bundlePack = Collector_.GenerateBundlePackInfo(builder.Target);
            ApplyBundleInfo(bundlePack);
            
            var jsonText = bundlePack.ToJson();
            PathUtils.CreateDirectory(builder.GetResOutputPath());
            System.IO.File.WriteAllText(PathUtils.ConcatPath(builder.GetResOutputPath(), LiteConst.BundlePackFileName), jsonText);
        }
        
        private void ApplyBundleInfo(BundlePackInfo packInfo)
        {
            foreach (var buildInfo in packInfo.BundleList)
            {
                if (buildInfo.DependencyList.Contains(buildInfo.BundlePath))
                {
                    LEditorLog.Error($"loop reference : {buildInfo.BundlePath}");
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