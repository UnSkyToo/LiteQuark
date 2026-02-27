using System;

namespace LiteQuark.Runtime
{
    public abstract class BaseTimer : BaseObject, ITimer
    {
        private const int MaxTicksPerFrame = 10;

        public ulong ID => UniqueID;
        public bool IsDone => _repeatCount == 0;
        public bool IsPaused => _isPaused;
        public bool IsUnscaled { get; private set; }

        public override string DebugName => $"Timer<{_interval} - {_repeatCount}>";

        private readonly float _interval;
        private readonly Action _onTick;
        private readonly Action _onComplete;

        private bool _isPaused;
        private int _repeatCount;
        private float _time;
        private float _delayTime;
        
        protected BaseTimer(float interval, float delayTime, int repeatCount, Action onTick, Action onComplete, bool isUnscaled = false)
        {
            _interval = interval;
            _delayTime = delayTime;
            _repeatCount = repeatCount;
            _onTick = onTick;
            _onComplete = onComplete;
            IsUnscaled = isUnscaled;

            _isPaused = false;
            _time = 0;
        }

        public void Tick(float deltaTime)
        {
            if (_isPaused || IsDone)
            {
                return;
            }

            if (_delayTime > 0)
            {
                _delayTime -= deltaTime;
                if (_delayTime > 0)
                {
                    return;
                }
                
                deltaTime = -_delayTime;
                _delayTime = 0;
            }

            _time += deltaTime;
            
            if (_interval <= 0)
            {
                _time = 0;
                TriggerTick();
                return;
            }
            
            var tickCount = 0;
            while (_time >= _interval && !IsDone && tickCount < MaxTicksPerFrame)
            {
                _time -= _interval;
                TriggerTick();
                tickCount++;
            }
            
            if (tickCount >= MaxTicksPerFrame)
            {
                _time = 0;
            }
        }
        
        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        public void Cancel()
        {
            if (IsDone)
            {
                return;
            }

            _repeatCount = 0;
        }

        private void TriggerTick()
        {
            _onTick?.Invoke();

            if (_repeatCount > 0)
            {
                _repeatCount--;

                if (_repeatCount == 0)
                {
                    TriggerComplete();
                }
            }
        }

        private void TriggerComplete()
        {
            _onComplete?.Invoke();
        }
    }
}