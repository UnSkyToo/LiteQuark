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
            TimerList_.Foreach((timer, list, dt) =>
            {
                timer.Tick(dt);

                if (timer.IsEnd)
                {
                    list.Remove(timer);
                }
            }, TimerList_, deltaTime);
        }

        public ulong AddTimer(float interval, Action onTick, int repeatCount = 1)
        {
            return AddTimer(interval, onTick, null, repeatCount);
        }

        public ulong AddTimer(float interval, Action onTick, float totalTime)
        {
            interval = Mathf.Max(interval, 0.0001f);
            return AddTimer(interval, onTick, (int)(totalTime / interval));
        }

        public ulong AddTimer(float interval, Action onTick, Action onComplete, float totalTime)
        {
            interval = Mathf.Max(interval, 0.0001f);
            return AddTimer(interval, onTick, onComplete, (int)(totalTime / interval));
        }

        public ulong AddTimerWithFrame(int frameCount, Action onTick, int repeatCount = 1)
        {
            return AddTimer(frameCount * (1.0f / Application.targetFrameRate), onTick, repeatCount);
        }

        public ulong NextFrame(Action onTick)
        {
            return AddTimerWithFrame(1, onTick, 1);
        }
        
        public ulong AddTimer(float interval, Action onTick, Action onComplete, int repeatCount = 1)
        {
            var newTimer = new NormalTimer(interval, repeatCount, onTick, onComplete);
            TimerList_.Add(newTimer);
            return newTimer.ID;
        }
        
        public ITimer FindTimer(ulong id)
        {
            if (id == 0)
            {
                return null;
            }
            
            return TimerList_.ForeachReturn((timer, targetId) => timer.ID == targetId, id);
        }

        public void StopTimer(ulong id)
        {
            var timer = FindTimer(id);
            if (timer == null || timer.IsEnd)
            {
                return;
            }
            timer.Cancel();
        }
    }
}