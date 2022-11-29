using UnityEditor;

namespace LiteQuark.Editor
{
    public class ProjectBuildConfig
    {
        public BuildTarget Target { get; }
        public ResBuildConfig ResConfig { get; }
        public AppBuildConfig AppConfig { get; }

        public ProjectBuildConfig(BuildTarget target, ResBuildConfig resConfig, AppBuildConfig appConfig)
        {
            Target = target;
            ResConfig = resConfig;
            AppConfig = appConfig;
        }
    }
}