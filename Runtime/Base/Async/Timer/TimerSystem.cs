using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class TimerSystem : ISystem, ITick
    {
        public const int RepeatCountForever = -1;
        
        private readonly ListEx<ITimer> TimerList_ = new ListEx<ITimer>();

        public TimerSystem()
        {
        }

        public void Dispose()
        {
            TimerList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            TimerList_.Foreach((timer, dt) =>
            {
                timer.Tick(dt);

                if (timer.IsEnd)
                {
                    TimerList_.Remove(timer);
                }
            }, deltaTime);
        }

        public ITimer AddTimer(float interval, Action onTick, int repeatCount = RepeatCountForever)
        {
            return AddTimer(interval, onTick, null, repeatCount);
        }

        public ITimer AddTimer(float interval, Action onTick, float totalTime)
        {
            interval = Mathf.Max(interval, 0.0001f);
            return AddTimer(interval, onTick, (int)(totalTime / interval));
        }

        public ITimer AddTimer(float interval, Action onTick, Action onComplete, int repeatCount = RepeatCountForever)
        {
            var newTimer = new NormalTimer(interval, repeatCount, onTick, onComplete);
            TimerList_.Add(newTimer);
            return newTimer;
        }

        public ITimer AddTimer(float interval, Action onTick, Action onComplete, float totalTime)
        {
            interval = Mathf.Max(interval, 0.0001f);
            return AddTimer(interval, onTick, onComplete, (int)(totalTime / interval));
        }

        public ITimer AddTimerWithFrame(int frameCount, Action onTick, int repeatCount = RepeatCountForever)
        {
            return AddTimer(frameCount * (1.0f / Application.targetFrameRate), onTick, repeatCount);
        }

        public void CancelTimer(ITimer timer)
        {
            timer?.Cancel();
        }
    }
}