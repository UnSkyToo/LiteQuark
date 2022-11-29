using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Build asset bundle
    /// </summary>
    internal sealed class ResBuildFileStep : IBuildStep
    {
        public string Name => "Res Build Step";

        public void Execute(ProjectBuilder builder)
        {
            var outputPath = builder.GetResOutputPath();
            PathHelper.CreateDirectory(outputPath);
            BuildPipeline.BuildAssetBundles(outputPath, builder.ResConfig.Options, builder.Target);
            AssetDatabase.Refresh();
        }
    }
}