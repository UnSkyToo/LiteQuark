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
            ApplySetting(builder);
        }

        private void CleanOutputFile(ProjectBuilder builder)
        {
            if (builder.AppConfig.CleanBuildMode)
            {
                FileUtil.DeleteFileOrDirectory(builder.GetAppOutputPath());
                AssetDatabase.Refresh();
            }
        }

        private void ApplySetting(ProjectBuilder builder)
        {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(builder.Target);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.SetScriptingBackend(namedBuildTarget, builder.AppConfig.Backend);
            
            EditorUserBuildSettings.development = builder.AppConfig.IsDevelopmentBuild;
            if (builder.AppConfig.IsDevelopmentBuild)
            {
                builder.AppConfig.Options |= BuildOptions.Development;
            }
            else
            {
                builder.AppConfig.Options &= ~BuildOptions.Development;
            }
            PlayerSettings.applicationIdentifier = builder.AppConfig.Identifier;
            PlayerSettings.bundleVersion = builder.AppConfig.Version;
            PlayerSettings.productName = builder.AppConfig.ProduceName;
#if UNITY_ANDROID
            PlayerSettings.Android.bundleVersionCode = builder.AppConfig.BuildCode;
            PlayerSettings.Android.targetArchitectures = builder.AppConfig.Architecture;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = builder.AppConfig.IsAAB;
            PlayerSettings.Android.useAPKExpansionFiles = builder.AppConfig.IsAAB && builder.AppConfig.SplitApplicationBinary;
            EditorUserBuildSettings.androidCreateSymbols = builder.AppConfig.CreateSymbols;
#elif UNITY_IOS
            PlayerSettings.iOS.buildNumber = builder.AppConfig.BuildCode.ToString();
            PlayerSettings.iOS.targetDevice = builder.AppConfig.TargetDevice;
#endif

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}