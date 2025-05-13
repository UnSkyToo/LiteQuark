using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class DefaultLoggerRepository : LoggerRepositoryBase
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Dictionary<string, ILogger> _loggerCache;

        public DefaultLoggerRepository()
            : this(new DefaultLoggerFactory())
        {
            Name = "DefaultLoggerRepository";
        }
        
        public DefaultLoggerRepository(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _loggerCache = new Dictionary<string, ILogger>();
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            _loggerCache.Clear();
        }
        
        public override ILogger[] GetCurrentLoggers() 
        {
            lock(this)
            {
                var result = new List<ILogger>(_loggerCache.Count);
                
                foreach(var current in _loggerCache)
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

            return GetLogger(name, _loggerFactory);
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
                if (_loggerCache.ContainsKey(name))
                {
                    logger = _loggerCache[name] as LoggerBase;
                }
                
                if (logger == null) 
                {
                    logger = factory.CreateLogger(name);
                    if (logger != null)
                    {
                        logger.Repository = this;
                        _loggerCache.Add(name, logger);
                    }
                }

                return logger;
            }
        }
    }
}