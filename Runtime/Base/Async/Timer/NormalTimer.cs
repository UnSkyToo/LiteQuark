using System;

namespace LiteQuark.Runtime
{
    public sealed class NormalTimer : BaseTimer
    {
        public NormalTimer(float interval, int repeatCount, Action onTick, Action onComplete)
            : base(interval, repeatCount, onTick, onComplete)
        {
        }
    }
}