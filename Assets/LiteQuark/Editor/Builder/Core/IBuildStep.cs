namespace LiteQuark.Editor
{
    internal interface IBuildStep
    {
        string Name { get; }
        
        void Execute(ProjectBuilder builder);
    }
}