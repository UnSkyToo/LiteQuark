using System;
using System.IO;

namespace LiteQuark.Runtime
{
    public sealed class SimpleLogLayout : ILogLayout
    {
        public bool IgnoresException => true;
        
        public void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException(nameof(loggingEvent));
            }

            writer.Write(loggingEvent.Level.ToString());
            writer.Write(" - ");
            writer.Write(loggingEvent.Message);
            writer.WriteLine();
        }
    }
}