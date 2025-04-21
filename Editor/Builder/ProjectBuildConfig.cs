using UnityEditor;

namespace LiteQuark.Editor
{
    public class ProjectBuildConfig
    {
        public BuildTarget Target { get; }
        public string Version { get; }
        public ResBuildConfig ResConfig { get; }
        public AppBuildConfig AppConfig { get; }

        public ProjectBuildConfig(BuildTarget target, string version, ResBuildConfig resConfig, AppBuildConfig appConfig)
        {
            Target = target;
            Version = version;
            ResConfig = resConfig;
            AppConfig = appConfig;
        }
    }
}