using System;

namespace LiteQuark.Runtime
{
    public sealed class NormalTimer : BaseTimer
    {
        public NormalTimer(float interval, float delayTime, int repeatCount, Action onTick, Action onComplete)
            : base(interval, delayTime, repeatCount, onTick, onComplete)
        {
        }
    }
}