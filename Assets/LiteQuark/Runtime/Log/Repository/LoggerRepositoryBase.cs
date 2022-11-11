using System;

namespace LiteQuark.Runtime
{
    public abstract class LoggerRepositoryBase : ILoggerRepository
    {
        public string Name { get; set; }
        
        private LogLevel CurrentLevel_;

        protected LoggerRepositoryBase()
        {
            CurrentLevel_ = LogLevel.All;
        }

        public virtual void Dispose()
        {
            foreach(var logger in GetCurrentLoggers())
            {
                if (logger is LoggerBase loggerBase)
                {
                    loggerBase.RemoveAllAppender();
                }
            }
        }

        public virtual bool IsLevelEnable(LogLevel level)
        {
            return (CurrentLevel_ & level) == level;
        }

        public virtual void EnableLevel(LogLevel level, bool enabled)
        {
            if (enabled)
            {
                CurrentLevel_ |= level;
            }
            else
            {
                CurrentLevel_ &= (~level);
            }
        }
        
        public abstract ILogger[] GetCurrentLoggers();
        
        public abstract ILogger GetLogger(string name);
        public abstract ILogger GetLogger(string name, ILoggerFactory loggerFactory);
    }
}