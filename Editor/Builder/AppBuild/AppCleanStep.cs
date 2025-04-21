using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Clean streamingAssets
    /// </summary>
    internal sealed class AppCleanStep : IBuildStep
    {
        public string Name => "App Clean Step";

        public void Execute(ProjectBuilder builder)
        {
            FileUtil.DeleteFileOrDirectory(PathUtils.GetStreamingAssetsPath(LiteConst.Tag));
            FileUtil.DeleteFileOrDirectory(PathUtils.ConcatPath(PathUtils.GetStreamingAssetsPath(LiteConst.Tag), $"{LiteConst.Tag}.meta"));
            AssetDatabase.Refresh();
        }
    }
}