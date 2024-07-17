namespace LiteQuark.Editor
{
    internal class ProjectBuildResult
    {
        public bool IsSuccess { get; }
        public float ElapsedSeconds { get; }
        public string OutputPath { get; }

        public ProjectBuildResult(bool isSuccess, float elapsedSeconds, string outputPath)
        {
            IsSuccess = isSuccess;
            ElapsedSeconds = elapsedSeconds;
            OutputPath = outputPath;
        }
    }
}