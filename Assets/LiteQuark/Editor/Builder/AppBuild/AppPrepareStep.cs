using LiteQuark.Runtime;
using UnityEngine;
using UnityEditor;

namespace LiteQuark.Editor
{
    internal sealed class AppPrepareStep : IBuildStep
    {
        public string Name => "App Prepare Step";

        public void Execute(ProjectBuilder builder)
        {
            CleanOutputFile(builder);
            CopyResToStreamingAssets(builder);
            ApplySetting(builder);
        }

        private void CopyResToStreamingAssets(ProjectBuilder builder)
        {
            PathHelper.CopyDirectory(builder.GetResOutputPath(), Application.streamingAssetsPath, CopyFilter);
            AssetDatabase.Refresh();
        }
        
        private bool CopyFilter(string path)
        {
            if (path.EndsWith(".manifest"))
            {
                return false;
            }

            return true;
        }

        private void CleanOutputFile(ProjectBuilder builder)
        {
            FileUtil.DeleteFileOrDirectory(builder.GetAppOutputPath());
        }

        private void ApplySetting(ProjectBuilder builder)
        {
            // var targetGroup = BuildPipeline.GetBuildTargetGroup(builder.Target);
            // PlayerSettings.SetApplicationIdentifier(targetGroup, "com.lite.quark.demo");
            // PlayerSettings.companyName = "lite";
            // PlayerSettings.productName = "demo";
            // PlayerSettings.bundleVersion = "1.0.0";
            //
            // EditorUserBuildSettings.development = false;
            //
            // AssetDatabase.SaveAssets();
        }
    }
}