namespace LiteQuark.Runtime
{
    public abstract class LoggerFactoryBase : ILoggerFactory
    {
        protected LoggerFactoryBase()
        {
        }

        public abstract LoggerBase CreateLogger(string name);

        public virtual LoggerBase CreateLogger(string name, ILogAppender[] appenderList)
        {
            var logger = new LoggerImpl(name);

            foreach (var appender in appenderList)
            {
                logger.AddAppender(appender);
            }

            return logger;
        }
        
        internal sealed class LoggerImpl : LoggerBase
        {
            internal LoggerImpl(string name)
                : base(name)
            {
            }
        }
    }
}