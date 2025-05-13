using System;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class TimerSystem : ISystem, ITick
    {
        public const int RepeatCountForever = -1;

        private readonly float FrameInterval_ = 0.01f;
        private readonly ListEx<ITimer> TimerList_ = new ListEx<ITimer>();

        public TimerSystem()
        {
            FrameInterval_ = MathF.Max(0.01f, 1.0f / LiteRuntime.Setting.Common.TargetFrameRate);
        }
        
        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            TimerList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            TimerList_.Foreach(OnTimerTick, TimerList_, deltaTime);
        }

        private void OnTimerTick(ITimer timer, ListEx<ITimer> list, float dt)
        {
            timer.Tick(dt);

            if (timer.IsEnd)
            {
                list.Remove(timer);
            }
        }
        
        public ulong AddTimer(float interval, Action onTick, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            return AddTimer(interval, onTick, (int)(totalTime / interval));
        }
        
        public ulong AddTimer(float interval, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return CreateTimer(interval, delayTime, onTick, null, repeatCount);
        }
        
        public ulong AddTimer(float interval, Action onTick, Action onComplete, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            return CreateTimer(interval, 0f, onTick, onComplete, (int)(totalTime / interval));
        }

        public ulong AddTimerWithFrame(int frameCount, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return AddTimer(frameCount * FrameInterval_, onTick, repeatCount, delayTime);
        }

        public ulong NextFrame(Action onTick)
        {
            return AddTimer(0, onTick, 1);
        }
        
        private ulong CreateTimer(float interval, float delayTime, Action onTick, Action onComplete, int repeatCount = 1)
        {
            var newTimer = new NormalTimer(interval, delayTime, repeatCount, onTick, onComplete);
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