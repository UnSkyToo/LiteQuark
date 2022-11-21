using System;
using System.Globalization;

namespace LiteQuark.Runtime
{
    public interface ILog
    {
        ILogger Logger { get; }
        
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }

        public void EnableLevel(LogLevel level, bool enabled);
        
        void Info(string message);
        void Info(string format, params object[] args);
        
        void Warn(string message);
        void Warn(string format, params object[] args);
        
        void Error(string message);
        void Error(string format, params object[] args);
        
        void Fatal(string message);
        void Fatal(string message, Exception exception);
        void Fatal(string format, params object[] args);
    }
    
    public sealed class LogImpl : ILog
    {
        public ILogger Logger { get; }

        public bool IsInfoEnabled => Logger.IsLevelEnable(LogLevel.Info);
        public bool IsWarnEnabled => Logger.IsLevelEnable(LogLevel.Warn);
        public bool IsErrorEnabled => Logger.IsLevelEnable(LogLevel.Error);
        public bool IsFatalEnabled => Logger.IsLevelEnable(LogLevel.Fatal);

        public LogImpl(ILogger logger)
        {
            Logger = logger;
        }

        public void EnableLevel(LogLevel level, bool enabled)
        {
            Logger.EnableLevel(level, enabled);
        }
        
        public void Info(string message)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(LogLevel.Info, message, null);
            }
        }

        public void Info(string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(LogLevel.Info, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void Warn(string message)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(LogLevel.Warn, message, null);
            }
        }

        public void Warn(string format, params object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(LogLevel.Warn, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void Error(string message)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(LogLevel.Error, message, null);
            }
        }

        public void Error(string format, params object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(LogLevel.Error, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void Fatal(string message)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(LogLevel.Fatal, message, null);
            }
        }

        public void Fatal(string message, Exception exception)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(LogLevel.Fatal, message, exception);
            }
        }

        public void Fatal(string format, params object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(LogLevel.Fatal, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }
        }
    }
}