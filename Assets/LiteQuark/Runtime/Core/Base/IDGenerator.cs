using System;

namespace LiteQuark.Runtime
{
    public static class IDGenerator
    {
        private const int TimestampBits = 50;
        private const int CounterBits = 14;

        private const int MaxCounter = 1 << CounterBits;
        
        private static readonly object Lock = new object();
        private static long _lastTimestamp;
        private static int _counter;
        
        public static ulong NextID()
        {
            lock (Lock)
            {
                var currentTimestamp = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
                if (currentTimestamp > _lastTimestamp)
                {
                    _lastTimestamp = currentTimestamp;
                    _counter = 1;
                }
                else
                {
                    _counter++;
                    if (_counter >= MaxCounter)
                    {
                        _counter = 1;
                        _lastTimestamp++;
                    }
                }

                return ((ulong)_lastTimestamp << CounterBits) | (uint)_counter;
            }
        }
    }
}