namespace LiteQuark.Editor
{
    public interface IBuildCallback
    {
        void PreProjectBuild(ProjectBuildConfig config);
        
        void PreStepBuild(ProjectBuildConfig config, System.Type stepType, string stepName);
        void PostStepBuild(ProjectBuildConfig config, System.Type stepType, string stepName);
        
        void PostProjectBuild(ProjectBuildConfig config);
    }
}