namespace LiteQuark.Runtime
{
    public interface ILogFilter
    {
        LogFilterDecision DoFilter(LoggingEvent loggingEvent);
    }
}