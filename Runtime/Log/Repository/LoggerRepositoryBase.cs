namespace LiteQuark.Runtime
{
    public abstract class LoggerRepositoryBase : ILoggerRepository
    {
        public string Name { get; set; }
        
        private LogLevel _currentLevel;

        protected LoggerRepositoryBase()
        {
            _currentLevel = LogLevel.All;
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
            return (_currentLevel & level) == level;
        }

        public virtual void EnableLevel(LogLevel level, bool enabled)
        {
            if (enabled)
            {
                _currentLevel |= level;
            }
            else
            {
                _currentLevel &= (~level);
            }
        }
        
        public abstract ILogger[] GetCurrentLoggers();
        
        public abstract ILogger GetLogger(string name);
        public abstract ILogger GetLogger(string name, ILoggerFactory loggerFactory);
    }
}