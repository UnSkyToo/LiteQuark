using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class LogSystem : ISystem
    {
        private ILoggerRepository Repository_ = null;
        private Dictionary<string, ILog> LogCache_ = null;
        private ILog CommonLogger_ = null;

        public LogSystem()
        {
        }

        public Task<bool> Initialize()
        {
            Repository_ = new DefaultLoggerRepository();
            LogCache_ = new Dictionary<string, ILog>();
            CommonLogger_ = GetLogger("Default");

            var setting = LiteRuntime.Setting.Log;
            var logEnable = setting.ReceiveLog && LiteRuntime.IsDebugMode;
            if (logEnable)
            {
                Repository_.EnableLevel(LogLevel.Info, setting.LogInfo);
                Repository_.EnableLevel(LogLevel.Warn, setting.LogWarn);
                Repository_.EnableLevel(LogLevel.Error, setting.LogError);
                Repository_.EnableLevel(LogLevel.Fatal, setting.LogFatal);
                UnityEngine.Debug.unityLogger.logEnabled = true;
            }
            else
            {
                Repository_.EnableLevel(LogLevel.All, false);
                UnityEngine.Debug.LogWarning("DebugMode is false, disable all log!");
                UnityEngine.Debug.unityLogger.logEnabled = false;
            }

            return Task.FromResult(true);
        }

        public void Dispose()
        {
            LogCache_.Clear();
            
            if (Repository_ != null)
            {
                Repository_.Dispose();
                Repository_ = null;
            }
        }

        public ILoggerRepository GetRepository()
        {
            return Repository_;
        }

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            if (LogCache_.TryGetValue(name, out var log))
            {
                return log;
            }
            
            var logger = Repository_.GetLogger(name);
            log = new LogImpl(logger, LiteRuntime.Setting.Log.SimpleLog);
            LogCache_.Add(name, log);
            return log;
        }

        public ILog GetLogger(string name, ILoggerFactory loggerFactory)
        {
            if (LogCache_.TryGetValue(name, out var log))
            {
                return log;
            }
            
            var logger = Repository_.GetLogger(name, loggerFactory);
            log = new LogImpl(logger, LiteRuntime.Setting.Log.SimpleLog);
            LogCache_.Add(name, log);
            return log;
        }

        public void EnableLevel(LogLevel level, bool enabled)
        {
            CommonLogger_.EnableLevel(level, enabled);
        }

        public void Info(string message)
        {
            CommonLogger_.Info(message);
        }

        public void Info(string format, params object[] args)
        {
            CommonLogger_.Info(format, args);
        }

        public void Warn(string message)
        {
            CommonLogger_.Warn(message);
        }

        public void Warn(string format, params object[] args)
        {
            CommonLogger_.Warn(format, args);
        }

        public void Error(string message)
        {
            CommonLogger_.Error(message);
        }

        public void Error(string format, params object[] args)
        {
            CommonLogger_.Error(format, args);
        }

        public void Fatal(string message)
        {
            CommonLogger_.Fatal(message);
        }

        public void Fatal(string message, Exception exception)
        {
            CommonLogger_.Fatal(message, exception);
        }

        public void Fatal(string format, params object[] args)
        {
            CommonLogger_.Fatal(format, args);
        }
    }
}