namespace LiteQuark.Editor
{
    internal struct ProjectBuildReport
    {
        public bool IsSuccess { get; set; }
        public string ErrorInfo { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public float ElapsedSeconds { get; set; }
        public string OutputRootPath { get; set; }
        public string OutputResPath { get; set; }
        public string OutputAppPath { get; set; }
    }
}