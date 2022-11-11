namespace LiteQuark.Runtime
{
    public interface ILoggerFactory
    {
        LoggerBase CreateLogger(string name);
        LoggerBase CreateLogger(string name, ILogAppender[] appenderList);
    }
}