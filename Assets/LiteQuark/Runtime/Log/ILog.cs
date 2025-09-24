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
        void Fatal(Exception exception, string message);
        void Fatal(Exception exception, string format, params object[] args);
    }
    
    public sealed class LogImpl : ILog
    {
        public ILogger Logger { get; }
        public bool SimpleLog { get; }

        public bool IsInfoEnabled => Logger.IsLevelEnable(LogLevel.Info);
        public bool IsWarnEnabled => Logger.IsLevelEnable(LogLevel.Warn);
        public bool IsErrorEnabled => Logger.IsLevelEnable(LogLevel.Error);
        public bool IsFatalEnabled => Logger.IsLevelEnable(LogLevel.Fatal);

        public LogImpl(ILogger logger, bool simpleLog)
        {
            Logger = logger;
            SimpleLog = simpleLog;
        }

        public void EnableLevel(LogLevel level, bool enabled)
        {
            Logger.EnableLevel(level, enabled);
        }
        
        public void Info(string message)
        {
            if (IsInfoEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.Log(message);
                }
                else
                {
                    Logger.Log(LogLevel.Info, message, null);
                }
            }
        }

        public void Info(string format, params object[] args)
        {
            if (IsInfoEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogFormat(format, args);
                }
                else
                {
                    Logger.Log(LogLevel.Info, string.Format(CultureInfo.InvariantCulture, format, args), null);
                }
            }
        }

        public void Warn(string message)
        {
            if (IsWarnEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogWarning(message);
                }
                else
                {
                    Logger.Log(LogLevel.Warn, message, null);
                }
            }
        }

        public void Warn(string format, params object[] args)
        {
            if (IsWarnEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogWarningFormat(format, args);
                }
                else
                {
                    Logger.Log(LogLevel.Warn, string.Format(CultureInfo.InvariantCulture, format, args), null);
                }
            }
        }

        public void Error(string message)
        {
            if (IsErrorEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogError(message);
                }
                else
                {
                    Logger.Log(LogLevel.Error, message, null);
                }
            }
        }

        public void Error(string format, params object[] args)
        {
            if (IsErrorEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogErrorFormat(format, args);
                }
                else
                {
                    Logger.Log(LogLevel.Error, string.Format(CultureInfo.InvariantCulture, format, args), null);
                }
            }
        }

        public void Fatal(string message)
        {
            if (IsFatalEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogException(new Exception(message));
                }
                else
                {
                    Logger.Log(LogLevel.Fatal, message, null);
                }
            }
        }

        public void Fatal(Exception exception, string message)
        {
            if (IsFatalEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogException(exception);
                    UnityEngine.Debug.LogError(message);
                }
                else
                {
                    Logger.Log(LogLevel.Fatal, message, exception);
                }
            }
        }

        public void Fatal(Exception exception, string format, params object[] args)
        {
            if (IsFatalEnabled)
            {
                if (SimpleLog)
                {
                    UnityEngine.Debug.LogException(new Exception(string.Format(CultureInfo.InvariantCulture, format, args), exception));
                }
                else
                {
                    Logger.Log(LogLevel.Fatal, string.Format(CultureInfo.InvariantCulture, format, args), exception);
                }
            }
        }
    }
}