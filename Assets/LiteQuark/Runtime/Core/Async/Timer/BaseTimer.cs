using System;

namespace LiteQuark.Runtime
{
    public abstract class BaseTimer : BaseObject, ITimer
    {
        public ulong ID => UniqueID;
        public bool IsDone => _repeatCount == 0;

        public override string DebugName => $"Timer<{_interval} - {_repeatCount}>";

        private readonly float _interval;
        private readonly Action _onTick;
        private readonly Action _onComplete;

        private bool _isPaused;
        private int _repeatCount;
        private float _time;
        private float _delayTime;
        
        protected BaseTimer(float interval, float delayTime, int repeatCount, Action onTick, Action onComplete)
        {
            _interval = interval;
            _delayTime = delayTime;
            _repeatCount = repeatCount;
            _onTick = onTick;
            _onComplete = onComplete;

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
                return;
            }

            _time += deltaTime;
            if (_time >= _interval)
            {
                _time -= _interval;
                TriggerTick();
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

            Pause();
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