using System;

namespace LiteQuark.Runtime
{
    public class TextWriterLogAppender : LogAppenderBase
    {
        public bool ImmediateFlush { get; set; } = true;
        public override bool RequireLayout => true;
        
        protected LogQuietTextWriter QuiteWriter_;
        
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
            if (QuiteWriter_ != null)
            {
                RenderLoggingEvent(QuiteWriter_, loggingEvent);

                if (ImmediateFlush)
                {
                    QuiteWriter_.Flush();
                }
            }
        }
        
        protected virtual void Reset() 
        {
            CloseWriter();
            QuiteWriter_ = null;
        }
        
        protected virtual void CloseWriter() 
        {
            if (QuiteWriter_ != null) 
            {
                try 
                {
                    QuiteWriter_.Close();
                } 
                catch(Exception ex) 
                {
                    LogErrorHandler.Error($"Could not close writer [{QuiteWriter_}]", ex);
                }
            }
        }
    }
}