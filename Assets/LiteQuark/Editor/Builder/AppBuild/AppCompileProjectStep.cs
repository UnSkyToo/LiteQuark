namespace LiteQuark.Editor
{
    /// <summary>
    /// Compile project
    /// </summary>
    internal sealed class AppCompileProjectStep : IBuildStep
    {
        public string Name => "App Compile Project Step";
        
        public void Execute(ProjectBuilder builder)
        {
        }
    }
}