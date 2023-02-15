using System;
using System.Collections.Generic;
using System.IO;

namespace LiteQuark.Runtime
{
    public abstract class LogAppenderBase : ILogAppender
    {
        public string Name { get; set; }
        public ILogLayout Layout { get; set; }
        public virtual bool RequireLayout => false;
        
        private const int RenderBufferSize = 256;
        private const int RenderBufferMaxCapacity = 1024;
        
        private readonly List<ILogFilter> FilterList_ = null;
        private LogReusableStringWriter RenderWriter_ = null;

        private bool IsOpened_;
        private bool IsClosed_;
        private bool RecursiveGuard_;

        protected LogAppenderBase()
        {
            IsClosed_ = false;
            RecursiveGuard_ = false;

            FilterList_ = new List<ILogFilter>();
        }

        ~LogAppenderBase()
        {
            if (!IsClosed_)
            {
                return;
            }
            
            Close();
        }

        public void Open()
        {
            lock (this)
            {
                if (IsOpened_)
                {
                    return;
                }

                OnOpen();
                IsOpened_ = true;
            }
        }

        protected virtual void OnOpen()
        {
        }
        
        public void Close()
        {
            lock (this)
            {
                if (IsClosed_)
                {
                    return;
                }

                OnClose();
                IsClosed_ = true;
            }
        }

        protected virtual void OnClose()
        {
        }

        public virtual void AddFilter(ILogFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException($"{nameof(filter)} param must not be null");
            }

            FilterList_.Add(filter);
        }
        
        public virtual void ClearFilterList()
        {
            FilterList_.Clear();
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            lock(this)
            {
                if (IsClosed_)
                {
                    LogErrorHandler.Error($"Attempted to append to closed appender named [{Name}].");
                    return;
                }

                // prevent re-entry
                if (RecursiveGuard_)
                {
                    return;
                }

                try
                {
                    RecursiveGuard_ = true;

                    if (FilterEvent(loggingEvent) && PreAppendCheck())
                    {
                        this.Append(loggingEvent);
                    }
                }
                catch(Exception ex)
                {
                    LogErrorHandler.Error("Failed in DoAppend", ex);
                }
                finally
                {
                    RecursiveGuard_ = false;
                }
            }
        }
        
        protected virtual bool FilterEvent(LoggingEvent loggingEvent)
        {
            foreach (var filter in FilterList_)
            {
                var decision = filter.DoFilter(loggingEvent);
                switch (decision)
                {
                    case LogFilterDecision.Deny:
                        return false;
                    case LogFilterDecision.Accept:
                        return true;
                    case LogFilterDecision.Neutral:
                        break;
                }
            }

            return true;
        }
        
        protected virtual bool PreAppendCheck()
        {
            if ((Layout == null) && RequireLayout)
            {
                LogErrorHandler.Error($"AppenderBase: No layout set for the appender named [{Name}].");
                return false;
            }

            return true;
        }
        
        protected abstract void Append(LoggingEvent loggingEvent);
        
        protected string RenderLoggingEvent(LoggingEvent loggingEvent)
        {
            if (RenderWriter_ == null)
            {
                RenderWriter_ = new LogReusableStringWriter(System.Globalization.CultureInfo.InvariantCulture);
            }

            lock (RenderWriter_)
            {
                // Reset the writer so we can reuse it
                RenderWriter_.Reset(RenderBufferMaxCapacity, RenderBufferSize);

                RenderLoggingEvent(RenderWriter_, loggingEvent);
                return RenderWriter_.ToString();
            }
        }
        
        protected void RenderLoggingEvent(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (Layout == null) 
            {
                throw new InvalidOperationException("A layout must be set");
            }

            if (Layout.IgnoresException) 
            {
                var exceptionStr = loggingEvent.GetExceptionString();
                if (exceptionStr != null && exceptionStr.Length > 0)
                {
                    // render the event and the exception
                    Layout.Format(writer, loggingEvent);
                    writer.WriteLine(exceptionStr);
                }
                else 
                {
                    // there is no exception to render
                    Layout.Format(writer, loggingEvent);
                }
            }
            else 
            {
                // The layout will render the exception
                Layout.Format(writer, loggingEvent);
            }
        }
    }
}