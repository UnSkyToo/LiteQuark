using System;

namespace LiteQuark.Runtime
{
    public class TextWriterLogAppender : LogAppenderBase
    {
        public bool ImmediateFlush { get; set; } = true;
        public override bool RequireLayout => true;
        
        protected LogQuietTextWriter QuiteWriter;
        
        public TextWriterLogAppender()
        {
        }

        protected override void OnClose()
        {
            lock (this)
            {
                Reset();
            }
        }

        protected override void Append(LoggingEvent loggingEvent) 
        {
            if (QuiteWriter != null)
            {
                RenderLoggingEvent(QuiteWriter, loggingEvent);

                if (ImmediateFlush)
                {
                    QuiteWriter.Flush();
                }
            }
        }
        
        protected virtual void Reset() 
        {
            CloseWriter();
            QuiteWriter = null;
        }
        
        protected virtual void CloseWriter() 
        {
            if (QuiteWriter != null) 
            {
                try 
                {
                    QuiteWriter.Close();
                } 
                catch(Exception ex) 
                {
                    LogErrorHandler.Error($"Could not close writer [{QuiteWriter}]", ex);
                }
            }
        }
    }
}