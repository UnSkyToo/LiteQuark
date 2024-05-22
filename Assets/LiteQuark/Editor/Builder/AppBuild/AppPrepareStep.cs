using LiteQuark.Runtime;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

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
            ProjectBuilderUtils.CopyToStreamingAssets(builder.GetResOutputPath());
        }

        private void CleanOutputFile(ProjectBuilder builder)
        {
            FileUtil.DeleteFileOrDirectory(builder.GetAppOutputPath());
        }

        private void ApplySetting(ProjectBuilder builder)
        {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(builder.Target);
            // PlayerSettings.SetApplicationIdentifier(targetGroup, "com.lite.quark.demo");
            // PlayerSettings.companyName = "lite";
            // PlayerSettings.productName = "demo";
            // PlayerSettings.bundleVersion = "1.0.0";
            //
            EditorUserBuildSettings.development = builder.AppConfig.IsDevelopmentBuild;

            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.SetScriptingBackend(namedBuildTarget, builder.AppConfig.Backend);
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            AssetDatabase.SaveAssets();
        }
    }
}