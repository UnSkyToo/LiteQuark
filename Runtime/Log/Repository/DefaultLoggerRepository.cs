using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class DefaultLoggerRepository : LoggerRepositoryBase
    {
        private readonly ILoggerFactory LoggerFactory_;
        private readonly Dictionary<string, ILogger> LoggerCache_;

        public DefaultLoggerRepository()
            : this(new DefaultLoggerFactory())
        {
        }
        
        public DefaultLoggerRepository(ILoggerFactory loggerFactory)
        {
            LoggerFactory_ = loggerFactory;
            LoggerCache_ = new Dictionary<string, ILogger>();
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            LoggerCache_.Clear();
        }
        
        public override ILogger[] GetCurrentLoggers() 
        {
            lock(this)
            {
                var result = new List<ILogger>(LoggerCache_.Count);
                
                foreach(var current in LoggerCache_)
                {
                    if (current.Value is LoggerBase logger) 
                    {
                        result.Add(logger);
                    }
                }

                return result.ToArray();
            }
        }
        
        public override ILogger GetLogger(string name) 
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return GetLogger(name, LoggerFactory_);
        }
        
        public override ILogger GetLogger(string name, ILoggerFactory factory) 
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock(this)
            {
                LoggerBase logger = null;
                if (LoggerCache_.ContainsKey(name))
                {
                    logger = LoggerCache_[name] as LoggerBase;
                }
                
                if (logger == null) 
                {
                    logger = factory.CreateLogger(name);
                    if (logger != null)
                    {
                        logger.Repository = this;
                        LoggerCache_.Add(name, logger);
                    }
                }

                return logger;
            }
        }
    }
}