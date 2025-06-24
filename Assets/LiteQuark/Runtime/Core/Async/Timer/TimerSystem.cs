using System;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class TimerSystem : ISystem, ITick
    {
        public const int RepeatCountForever = -1;

        private readonly float _frameInterval = 0.01f;
        private readonly Action<ITimer, ListEx<ITimer>, float> _onTickDelegate;
        private readonly ListEx<ITimer> _timerList = new ListEx<ITimer>();

        public TimerSystem()
        {
            _frameInterval = MathF.Max(0.01f, 1.0f / LiteRuntime.Setting.Common.TargetFrameRate);
            _onTickDelegate = OnTimerTick;
        }
        
        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            _timerList.Clear();
        }

        public void Tick(float deltaTime)
        {
            _timerList.Foreach(_onTickDelegate, _timerList, deltaTime);
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
            return AddTimer(frameCount * _frameInterval, onTick, repeatCount, delayTime);
        }

        public ulong NextFrame(Action onTick)
        {
            return AddTimer(0, onTick, 1);
        }
        
        private ulong CreateTimer(float interval, float delayTime, Action onTick, Action onComplete, int repeatCount = 1)
        {
            var newTimer = new NormalTimer(interval, delayTime, repeatCount, onTick, onComplete);
            _timerList.Add(newTimer);
            return newTimer.ID;
        }
        
        public ITimer FindTimer(ulong id)
        {
            if (id == 0)
            {
                return null;
            }
            
            return _timerList.ForeachReturn((timer, targetId) => timer.ID == targetId, id);
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