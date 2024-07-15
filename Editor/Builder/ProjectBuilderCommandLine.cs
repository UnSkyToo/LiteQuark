using UnityEditor;

namespace LiteQuark.Editor
{
    public static class ProjectBuilderCommandLine
    {
        public static bool BuildProject(BuildTarget target, ResBuildConfig resCfg, AppBuildConfig appCfg)
        {
            var buildCfg = new ProjectBuildConfig(target, resCfg, appCfg);
            var result = new ProjectBuilder().Build(buildCfg);
            return result.IsSuccess;
        }
    }
}