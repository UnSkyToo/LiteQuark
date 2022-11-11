using System;
using System.IO;
using System.Text;

namespace LiteQuark.Runtime
{
    public sealed class LogQuietTextWriter : TextWriter
    {
        public override Encoding Encoding => Writer_.Encoding;
        
        private readonly TextWriter Writer_;
        
        public LogQuietTextWriter(TextWriter writer)
        {
            Writer_ = writer;
        }

        public override void Flush()
        {
            Writer_.Flush();
        }

        public override void Write(char value) 
        {
            try 
            {
                Writer_.Write(value);
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
                Writer_.Write(buffer, index, count);
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
                Writer_.Write(value);
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
                Writer_.WriteLine();
            } 
            catch(Exception ex) 
            {
                LogErrorHandler.Error($"Failed to write ['/n'].", ex);
            }
        }
    }
}