using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class LogManager : Singleton<LogManager>, IManager
    {
        private ILoggerRepository Repository_ = null;
        private Dictionary<string, ILog> LogCache_ = null;
        private ILog CommonLogger_ = null;

        public bool Startup()
        {
            Repository_ = new DefaultLoggerRepository();
            LogCache_ = new Dictionary<string, ILog>();
            CommonLogger_ = GetLogger("Default");
            return true;
        }

        public void Shutdown()
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
            log = new LogImpl(logger);
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
            log = new LogImpl(logger);
            LogCache_.Add(name, log);
            return log;
        }

        public void Info(string message)
        {
            CommonLogger_.Info(message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            CommonLogger_.InfoFormat(format, args);
        }

        public void Warn(string message)
        {
            CommonLogger_.Warn(message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            CommonLogger_.WarnFormat(format, args);
        }

        public void Error(string message)
        {
            CommonLogger_.Error(message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            CommonLogger_.ErrorFormat(format, args);
        }

        public void Fatal(string message)
        {
            CommonLogger_.Fatal(message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            CommonLogger_.FatalFormat(format, args);
        }
    }
}