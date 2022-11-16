using System;

namespace LiteQuark.Runtime
{
    [Flags]
    public enum LogLevel : byte
    {
        Info    = 1 << 0,
        Warn    = 1 << 1,
        Error   = 1 << 2,
        Fatal   = 1 << 3,
        All     = 0xFF,
    }
}