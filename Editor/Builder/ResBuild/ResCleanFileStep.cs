using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Clean last build asset bundle file
    /// </summary>
    internal sealed class ResCleanFileStep : IBuildStep
    {
        public string Name => "Res Clean File Step";

        public void Execute(ProjectBuilder builder)
        {
            FileUtil.DeleteFileOrDirectory(builder.GetResOutputPath());
            AssetDatabase.Refresh();
        }
    }
}