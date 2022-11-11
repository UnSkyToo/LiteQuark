using System;

namespace LiteQuark.Runtime
{
    public interface ILogger
    {
        string Name { get; }
        
        bool IsLevelEnable(LogLevel level);
        void EnableLevel(LogLevel level, bool enabled);

        void Log(LogLevel level, string message, Exception exception);
        void Log(LoggingEvent evt);
    }
}