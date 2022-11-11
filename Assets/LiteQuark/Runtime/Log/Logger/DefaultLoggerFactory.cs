namespace LiteQuark.Runtime
{
    internal class DefaultLoggerFactory : LoggerFactoryBase
    {
        internal DefaultLoggerFactory()
            : base()
        {
        }
        
        public override LoggerBase CreateLogger(string name)
        {
            var unityConsoleAppender = new UnityConsoleLogAppender();
            unityConsoleAppender.Name = nameof(UnityConsoleLogAppender);
            unityConsoleAppender.Layout = new SimpleLogLayout();

            // var fileAppender = new FileLogAppender(UnityEngine.Application.persistentDataPath, false);
            // fileAppender.Name = name;
            // fileAppender.Layout = new SimpleLogLayout();
            
            return CreateLogger(name, new ILogAppender[] { unityConsoleAppender/*, fileAppender */});
        }
    }
}