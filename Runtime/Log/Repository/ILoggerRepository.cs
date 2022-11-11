using System;

namespace LiteQuark.Runtime
{
    public interface ILoggerRepository : IDisposable
    {
        string Name { get; set; }

        bool IsLevelEnable(LogLevel level);
        void EnableLevel(LogLevel level, bool enabled);
        
        ILogger[] GetCurrentLoggers();
        ILogger GetLogger(string name);
        ILogger GetLogger(string name, ILoggerFactory loggerFactory);
    }
}