using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Switch platform to target
    /// </summary>
    internal sealed class SwitchPlatformStep : IBuildStep
    {
        public string Name => "Switch Platform Step";

        public void Execute(ProjectBuilder builder)
        {
            if (EditorUserBuildSettings.activeBuildTarget != builder.Target)
            {
                var targetGroup = BuildPipeline.GetBuildTargetGroup(builder.Target);
                EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, builder.Target);
                AssetDatabase.Refresh();
            }
        }
    }
}