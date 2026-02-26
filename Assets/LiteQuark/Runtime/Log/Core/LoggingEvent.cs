using System;
using System.Diagnostics;
using System.Threading;

namespace LiteQuark.Runtime
{
    public class LoggingEvent
    {
        public string LoggerName { get; }
        public LogLevel Level { get; }
        public string Message { get; }
        public Exception ThrowException { get; }
        public string Trace { get; }
        public string ThreadName { get; }
        public DateTime TimeStamp { get; }
        
        public LoggingEvent(string loggerName, LogLevel level, string message, Exception exception)
            : this(loggerName, level, message, exception, level != LogLevel.Info ? new StackTrace().ToString() : string.Empty, Thread.CurrentThread.Name, DateTime.Now)
        {
        }

        public LoggingEvent(string loggerName, LogLevel level, string message, Exception exception, string trace, string threadName, DateTime timeStamp)
        {
            LoggerName = loggerName;
            Level = level;
            Message = message;
            ThrowException = exception;
            Trace = trace;
            ThreadName = threadName;
            TimeStamp = timeStamp;
        }
        
        public string GetExceptionString()
        {
            if (ThrowException != null)
            {
                return ThrowException.ToString();
            }

            return string.Empty;
        }
    }
}