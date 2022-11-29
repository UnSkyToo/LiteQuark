using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Clean last build asset bundle info
    /// </summary>
    internal sealed class ResCleanInfoStep : IBuildStep
    {
        public string Name => "Res Clean Info Step";

        public void Execute(ProjectBuilder builder)
        {
            var assetBundleNameList = AssetDatabase.GetAllAssetBundleNames();
            foreach (var assetBundleName in assetBundleNameList)
            {
                var assetPathList = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                foreach (var assetPath in assetPathList)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    importer.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
                }
            }
            
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }
    }
}