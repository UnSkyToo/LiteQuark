namespace LiteQuark.Runtime
{
    public sealed class UnityConsoleLogAppender : LogAppenderBase
    {
        public override bool RequireLayout => true;
        
        public UnityConsoleLogAppender()
        {
        }
        
        protected override void Append(LoggingEvent loggingEvent)
        {
            var msg = RenderLoggingEvent(loggingEvent);
            
            switch (loggingEvent.Level)
            {
                case LogLevel.Info:
                    UnityEngine.Debug.Log(msg);
                    break;
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning(msg);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(msg);
                    break;
                case LogLevel.Fatal:
                    UnityEngine.Debug.LogError(msg);
                    break;
                default:
                    break;
            }
        }
    }
}