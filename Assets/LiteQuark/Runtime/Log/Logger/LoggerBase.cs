using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public abstract class LoggerBase : ILogger
    {
        public string Name { get; }
        public ILoggerRepository Repository { get; set; }

        private LogLevel _currentLevel;
        private readonly List<ILogAppender> _appenderList;

        protected LoggerBase(string name)
        {
            Name = name;
            _currentLevel = LogLevel.All;
            _appenderList = new List<ILogAppender>();
        }

        public void AddAppender(ILogAppender appender)
        {
            appender.Open();
            _appenderList.Add(appender);
        }

        public void RemoveAppender(ILogAppender appender)
        {
            appender.Close();
            _appenderList.Remove(appender);
        }

        public void RemoveAllAppender()
        {
            foreach (var appender in _appenderList)
            {
                appender.Close();
            }
            
            _appenderList.Clear();
        }

        public bool IsLevelEnable(LogLevel level)
        {
            if (Repository != null && !Repository.IsLevelEnable(level))
            {
                return false;
            }
            
            return (_currentLevel & level) == level;
        }

        public void EnableLevel(LogLevel level, bool enabled)
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

        public void Log(LogLevel level, string message, Exception exception)
        {
            try
            {
                if (!IsLevelEnable(level))
                {
                    return;
                }

                ForcedLog(level, message, exception);
            }
            catch (Exception ex)
            {
                LogErrorHandler.Error("Exception while logging", ex);
            }
        }

        public void Log(LoggingEvent logEvent)
        {
            try
            {
                if (logEvent == null || !IsLevelEnable(logEvent.Level))
                {
                    return;
                }

                ForcedLog(logEvent);
            }
            catch (Exception ex)
            {
                LogErrorHandler.Error("Exception while logging", ex);
            }
        }

        protected virtual void ForcedLog(LogLevel level, string message, Exception exception)
        {
            CallAppenderList(new LoggingEvent(Name, level, message, exception));
        }
        
        protected virtual void ForcedLog(LoggingEvent logEvent)
        {
            CallAppenderList(logEvent);
        }

        protected virtual void CallAppenderList(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException(nameof(loggingEvent));
            }

            lock (this)
            {
                foreach (var appender in _appenderList)
                {
                    appender.DoAppend(loggingEvent);
                }
            }
        }
    }
}