using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class TimerSystem : ISystem, ITick
    {
        public const int RepeatCountForever = -1;

        private readonly float _frameInterval = 0.01f;
        private readonly SafeList<ITimer> _timerList = new();

        public TimerSystem()
        {
            _frameInterval = MathF.Max(0.01f, 1.0f / LiteRuntime.Setting.Common.TargetFrameRate);
        }

        public UniTask<bool> Initialize(CancellationToken ct = default)
        {
            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            foreach (var timer in _timerList)
            {
                timer.Cancel();
            }

            _timerList.Clear();
            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            var unscaledDeltaTime = LiteTime.UnscaledDeltaTime;

            foreach (var timer in _timerList)
            {
                timer.Tick(timer.IsUnscaled ? unscaledDeltaTime : deltaTime);

                if (timer.IsDone)
                {
                    _timerList.Remove(timer);
                }
            }
        }

        public ITimer AddTimer(float interval, Action onTick, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            var repeatCount = (int)MathF.Round(totalTime / interval);
            return CreateTimer(interval, 0f, onTick, null, repeatCount);
        }

        public ITimer AddTimer(float interval, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return CreateTimer(interval, delayTime, onTick, null, repeatCount);
        }

        public ITimer AddTimer(float interval, Action onTick, Action onComplete, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            var repeatCount = (int)MathF.Round(totalTime / interval);
            return CreateTimer(interval, 0f, onTick, onComplete, repeatCount);
        }

        public ITimer AddUnscaledTimer(float interval, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return CreateTimer(interval, delayTime, onTick, null, repeatCount, true);
        }

        public ITimer AddUnscaledTimer(float interval, Action onTick, Action onComplete, float totalTime)
        {
            interval = MathF.Max(interval, 0.0001f);
            var repeatCount = (int)MathF.Round(totalTime / interval);
            return CreateTimer(interval, 0f, onTick, onComplete, repeatCount, true);
        }

        public ITimer AddTimerWithFrame(int frameCount, Action onTick, int repeatCount = 1, float delayTime = 0f)
        {
            return AddTimer(frameCount * _frameInterval, onTick, repeatCount, delayTime);
        }

        public ITimer NextFrame(Action onTick)
        {
            return AddTimer(0, onTick, 1);
        }

        private ITimer CreateTimer(float interval, float delayTime, Action onTick, Action onComplete, int repeatCount = 1, bool isUnscaled = false)
        {
            var timer = new NormalTimer(interval, delayTime, repeatCount, onTick, onComplete, isUnscaled);
            _timerList.Add(timer);
            return timer;
        }

        public ITimer FindTimer(ulong id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (var timer in _timerList)
            {
                if (timer.ID == id)
                {
                    return timer;
                }
            }

            return null;
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
            foreach (var timer in _timerList)
            {
                timer.Cancel();
            }
        }

        internal SafeList<ITimer> GetTimerList()
        {
            return _timerList;
        }
    }
}
