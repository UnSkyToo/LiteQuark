using System;

namespace LiteQuark.Runtime
{
    public static class IDGenerator
    {
        private const int TimestampBits = 50;
        private const int CounterBits = 14;

        private const int MaxCounter = 1 << CounterBits;
        
        private static readonly object Lock_ = new object();
        private static long LastTimestamp_;
        private static int Counter_;
        
        public static ulong NextID()
        {
            lock (Lock_)
            {
                var currentTimestamp = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
                if (currentTimestamp > LastTimestamp_)
                {
                    LastTimestamp_ = currentTimestamp;
                    Counter_ = 1;
                }
                else
                {
                    Counter_++;
                    if (Counter_ >= MaxCounter)
                    {
                        Counter_ = 1;
                        LastTimestamp_++;
                    }
                }

                return ((ulong)LastTimestamp_ << CounterBits) | (uint)Counter_;
            }
        }
    }
}