using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class LogSystem : ISystem
    {
        private ILoggerRepository _repository = null;
        private Dictionary<string, ILog> _logCache = null;
        private ILog _commonLogger = null;

        public LogSystem()
        {
        }

        public Task<bool> Initialize()
        {
            _repository = new DefaultLoggerRepository();
            _logCache = new Dictionary<string, ILog>();
            _commonLogger = GetLogger("Default");

            var setting = LiteRuntime.Setting.Log;
            var logEnable = setting.ReceiveLog && LiteRuntime.IsDebugMode;
            if (logEnable)
            {
                _repository.EnableLevel(LogLevel.Info, setting.LogInfo);
                _repository.EnableLevel(LogLevel.Warn, setting.LogWarn);
                _repository.EnableLevel(LogLevel.Error, setting.LogError);
                _repository.EnableLevel(LogLevel.Fatal, setting.LogFatal);
                UnityEngine.Debug.unityLogger.logEnabled = true;
            }
            else
            {
                _repository.EnableLevel(LogLevel.All, false);
                UnityEngine.Debug.LogWarning("DebugMode is false, disable all log!");
                UnityEngine.Debug.unityLogger.logEnabled = false;
            }

            return Task.FromResult(true);
        }

        public void Dispose()
        {
            _logCache.Clear();
            
            if (_repository != null)
            {
                _repository.Dispose();
                _repository = null;
            }
        }

        public ILoggerRepository GetRepository()
        {
            return _repository;
        }

        public ILog GetLogger(Type type)
        {
            return GetLogger(type.FullName);
        }

        public ILog GetLogger(string name)
        {
            if (_logCache.TryGetValue(name, out var log))
            {
                return log;
            }
            
            var logger = _repository.GetLogger(name);
            log = new LogImpl(logger, LiteRuntime.Setting.Log.SimpleLog);
            _logCache.Add(name, log);
            return log;
        }

        public ILog GetLogger(string name, ILoggerFactory loggerFactory)
        {
            if (_logCache.TryGetValue(name, out var log))
            {
                return log;
            }
            
            var logger = _repository.GetLogger(name, loggerFactory);
            log = new LogImpl(logger, LiteRuntime.Setting.Log.SimpleLog);
            _logCache.Add(name, log);
            return log;
        }

        public void EnableLevel(LogLevel level, bool enabled)
        {
            _commonLogger.EnableLevel(level, enabled);
        }

        public void Info(string message)
        {
            _commonLogger.Info(message);
        }

        public void Info(string format, params object[] args)
        {
            _commonLogger.Info(format, args);
        }

        public void Warn(string message)
        {
            _commonLogger.Warn(message);
        }

        public void Warn(string format, params object[] args)
        {
            _commonLogger.Warn(format, args);
        }

        public void Error(string message)
        {
            _commonLogger.Error(message);
        }

        public void Error(string format, params object[] args)
        {
            _commonLogger.Error(format, args);
        }

        public void Fatal(string message)
        {
            _commonLogger.Fatal(message);
        }

        public void Fatal(string message, Exception exception)
        {
            _commonLogger.Fatal(message, exception);
        }

        public void Fatal(string format, params object[] args)
        {
            _commonLogger.Fatal(format, args);
        }
    }
}