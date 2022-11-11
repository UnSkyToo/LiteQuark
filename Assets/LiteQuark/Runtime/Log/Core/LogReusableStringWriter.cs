using System;
using System.IO;

namespace LiteQuark.Runtime
{
    public class LogReusableStringWriter : StringWriter
    {
        public LogReusableStringWriter(IFormatProvider formatProvider)
            : base(formatProvider) 
        {
        }
        
        protected override void Dispose(bool disposing)
        {
        }
        
        public void Reset(int maxCapacity, int defaultSize)
        {
            // Reset working string buffer
            var sb = GetStringBuilder();

            sb.Length = 0;
			
            // Check if over max size
            if (sb.Capacity > maxCapacity) 
            {
                sb.Capacity = defaultSize;
            } 
        }
    }
}