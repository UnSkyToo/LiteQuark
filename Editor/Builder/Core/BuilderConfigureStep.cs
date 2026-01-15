using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Configure builder
    /// </summary>
    internal sealed class BuilderConfigureStep : IBuildStep
    {
        public string Name => "Builder Configure Step";

        public void Execute(ProjectBuilder builder)
        {
            if (EditorUserBuildSettings.activeBuildTarget != builder.Target)
            {
                var targetGroup = BuildPipeline.GetBuildTargetGroup(builder.Target);
                EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, builder.Target);
                AssetDatabase.Refresh();
            }
            
            var namedBuildTarget = LiteEditorUtils.GetNamedBuildTarget(builder.Target);
            PlayerSettings.SetScriptingBackend(namedBuildTarget, builder.AppConfig.Backend);
            PlayerSettings.SplashScreen.showUnityLogo = false;
            
            EditorUserBuildSettings.development = builder.AppConfig.IsDevelopmentBuild;
            PlayerSettings.applicationIdentifier = builder.AppConfig.Identifier;
            PlayerSettings.bundleVersion = builder.Version;
            PlayerSettings.productName = builder.AppConfig.ProduceName;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}