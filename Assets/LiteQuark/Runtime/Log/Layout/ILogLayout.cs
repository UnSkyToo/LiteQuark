using System.IO;

namespace LiteQuark.Runtime
{
    public interface ILogLayout
    {
        bool IgnoresException { get; }
        
        void Format(TextWriter writer, LoggingEvent loggingEvent);
    }
}