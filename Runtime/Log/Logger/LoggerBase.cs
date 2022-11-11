using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public abstract class LoggerBase : ILogger
    {
        public string Name { get; }
        public ILoggerRepository Repository { get; set; }

        private LogLevel CurrentLevel_;
        private readonly List<ILogAppender> AppenderList_;

        protected LoggerBase(string name)
        {
            Name = name;
            CurrentLevel_ = LogLevel.All;
            AppenderList_ = new List<ILogAppender>();
        }

        public void AddAppender(ILogAppender appender)
        {
            appender.Open();
            AppenderList_.Add(appender);
        }

        public void RemoveAppender(ILogAppender appender)
        {
            appender.Close();
            AppenderList_.Remove(appender);
        }

        public void RemoveAllAppender()
        {
            foreach (var appender in AppenderList_)
            {
                appender.Close();
            }
            
            AppenderList_.Clear();
        }

        public bool IsLevelEnable(LogLevel level)
        {
            if (Repository != null && !Repository.IsLevelEnable(level))
            {
                return false;
            }
            
            return (CurrentLevel_ & level) == level;
        }

        public void EnableLevel(LogLevel level, bool enabled)
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
                foreach (var appender in AppenderList_)
                {
                    appender.DoAppend(loggingEvent);
                }
            }
        }
    }
}