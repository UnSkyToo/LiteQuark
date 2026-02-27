using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class TimerSystem : ISystem, ITick
    {
        public const int RepeatCountForever = -1;

        private readonly float _frameInterval = 0.01f;
        private readonly Action<ITimer, SafeList<ITimer>, float, float> _onTickDelegate = null;
        private readonly SafeList<ITimer> _timerList = new SafeList<ITimer>();

        public TimerSystem()
        {
            _frameInterval = MathF.Max(0.01f, 1.0f / LiteRuntime.Setting.Common.TargetFrameRate);
            _onTickDelegate = OnTimerTick;
        }
        
        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            _timerList.Clear();
        }

        public void Tick(float deltaTime)
        {
            _timerList.Foreach(_onTickDelegate, _timerList, deltaTime, LiteTime.UnscaledDeltaTime);
        }
        
        private void OnTimerTick(ITimer timer, SafeList<ITimer> list, float dt, float unscaledDt)
        {
            timer.Tick(timer.IsUnscaled ? unscaledDt : dt);

            if (timer.IsDone)
            {
                list.Remove(timer);
            }
        }
        
        public ulong AddTimer(float interval, Action onTick, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            var repeatCount = (int)MathF.Round(totalTime / interval);
            return CreateTimer(interval, 0f, onTick, null, repeatCount);
        }
        
        public ulong AddTimer(float interval, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return CreateTimer(interval, delayTime, onTick, null, repeatCount);
        }
        
        public ulong AddTimer(float interval, Action onTick, Action onComplete, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            var repeatCount = (int)MathF.Round(totalTime / interval);
            return CreateTimer(interval, 0f, onTick, onComplete, repeatCount);
        }
        
        public ulong AddUnscaledTimer(float interval, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return CreateTimer(interval, delayTime, onTick, null, repeatCount, true);
        }
        
        public ulong AddUnscaledTimer(float interval, Action onTick, Action onComplete, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            var repeatCount = (int)MathF.Round(totalTime / interval);
            return CreateTimer(interval, 0f, onTick, onComplete, repeatCount, true);
        }

        public ulong AddTimerWithFrame(int frameCount, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return AddTimer(frameCount * _frameInterval, onTick, repeatCount, delayTime);
        }

        public ulong NextFrame(Action onTick)
        {
            return AddTimer(0, onTick, 1);
        }
        
        private ulong CreateTimer(float interval, float delayTime, Action onTick, Action onComplete, int repeatCount = 1, bool isUnscaled = false)
        {
            var timer = new NormalTimer(interval, delayTime, repeatCount, onTick, onComplete, isUnscaled);
            _timerList.Add(timer);
            return timer.ID;
        }
        
        public ITimer FindTimer(ulong id)
        {
            if (id == 0)
            {
                return null;
            }
            
            return _timerList.ForeachReturn(static (timer, targetId) => timer.ID == targetId, id);
        }

        public void StopTimer(ulong id)
        {
            var timer = FindTimer(id);
            if (timer == null || timer.IsDone)
            {
                return;
            }
            timer.Cancel();
        }

        public void CancelAll()
        {
            _timerList.Foreach(static timer => timer.Cancel());
        }

        internal SafeList<ITimer> GetTimerList()
        {
            return _timerList;
        }
    }
}