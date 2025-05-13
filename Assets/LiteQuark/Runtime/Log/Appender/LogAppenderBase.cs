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
        
        private readonly List<ILogFilter> _filterList = null;
        private LogReusableStringWriter _renderWriter = null;

        private bool _isOpened;
        private bool _isClosed;
        private bool _recursiveGuard;

        protected LogAppenderBase()
        {
            _isClosed = false;
            _recursiveGuard = false;

            _filterList = new List<ILogFilter>();
        }

        ~LogAppenderBase()
        {
            if (!_isClosed)
            {
                return;
            }
            
            Close();
        }

        public void Open()
        {
            lock (this)
            {
                if (_isOpened)
                {
                    return;
                }

                OnOpen();
                _isOpened = true;
            }
        }

        protected virtual void OnOpen()
        {
        }
        
        public void Close()
        {
            lock (this)
            {
                if (_isClosed)
                {
                    return;
                }

                OnClose();
                _isClosed = true;
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

            _filterList.Add(filter);
        }
        
        public virtual void ClearFilterList()
        {
            _filterList.Clear();
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            lock(this)
            {
                if (_isClosed)
                {
                    LogErrorHandler.Error($"Attempted to append to closed appender named [{Name}].");
                    return;
                }

                // prevent re-entry
                if (_recursiveGuard)
                {
                    return;
                }

                try
                {
                    _recursiveGuard = true;

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
                    _recursiveGuard = false;
                }
            }
        }
        
        protected virtual bool FilterEvent(LoggingEvent loggingEvent)
        {
            foreach (var filter in _filterList)
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
            if (_renderWriter == null)
            {
                _renderWriter = new LogReusableStringWriter(System.Globalization.CultureInfo.InvariantCulture);
            }

            lock (_renderWriter)
            {
                // Reset the writer so we can reuse it
                _renderWriter.Reset(RenderBufferMaxCapacity, RenderBufferSize);

                RenderLoggingEvent(_renderWriter, loggingEvent);
                return _renderWriter.ToString();
            }
        }
        
        protected void RenderLoggingEvent(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (Layout == null) 
            {
                throw new InvalidOperationException("A layout must be set");
            }

            if (!Layout.IgnoresException) 
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