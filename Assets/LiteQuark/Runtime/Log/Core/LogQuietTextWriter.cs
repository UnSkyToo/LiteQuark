using System;
using System.IO;
using System.Text;

namespace LiteQuark.Runtime
{
    public sealed class LogQuietTextWriter : TextWriter
    {
        public override Encoding Encoding => _writer.Encoding;
        
        private readonly TextWriter _writer;
        
        public LogQuietTextWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        public override void Write(char value) 
        {
            try 
            {
                _writer.Write(value);
            } 
            catch(Exception e) 
            {
                LogErrorHandler.Error($"Failed to write [{value}].", e);
            }
        }
        
        public override void Write(char[] buffer, int index, int count) 
        {
            try 
            {
                _writer.Write(buffer, index, count);
            } 
            catch(Exception ex) 
            {
                LogErrorHandler.Error("Failed to write buffer.", ex);
            }
        }
        
        public override void Write(string value) 
        {
            try 
            {
                _writer.Write(value);
            } 
            catch(Exception ex) 
            {
                LogErrorHandler.Error($"Failed to write [{value}].", ex);
            }
        }

        public override void WriteLine()
        {
            try 
            {
                _writer.WriteLine();
            } 
            catch(Exception ex) 
            {
                LogErrorHandler.Error($"Failed to write ['/n'].", ex);
            }
        }
    }
}