namespace LiteQuark.Runtime
{
    public interface ILogAppender
    {
        string Name { get; set; }

        void Open();
        void Close();
        void DoAppend(LoggingEvent loggingEvent);
    }
}