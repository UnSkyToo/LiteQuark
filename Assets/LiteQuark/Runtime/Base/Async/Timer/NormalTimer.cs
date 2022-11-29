using System;

namespace LiteQuark.Runtime
{
    public sealed class NormalTimer : ITimer
    {
        public bool IsEnd => RepeatCount_ == 0;
        
        private readonly float Interval_;
        private readonly Action OnTick_;
        private readonly Action OnComplete_;

        private bool IsPaused_;
        private int RepeatCount_;
        private float Time_;
        
        public NormalTimer(float interval, int repeatCount, Action onTick, Action onComplete)
        {
            Interval_ = interval;
            RepeatCount_ = repeatCount;
            OnTick_ = onTick;
            OnComplete_ = onComplete;

            IsPaused_ = false;
            Time_ = 0;
        }

        public void Tick(float deltaTime)
        {
            if (IsPaused_ || IsEnd)
            {
                return;
            }

            Time_ += deltaTime;
            if (Time_ >= Interval_)
            {
                Time_ -= Interval_;
                TriggerTick();
            }
        }
        
        public void Pause()
        {
            IsPaused_ = true;
        }

        public void Resume()
        {
            IsPaused_ = false;
        }

        public void Cancel()
        {
            if (IsEnd)
            {
                return;
            }

            Pause();
            RepeatCount_ = 0;
        }

        private void TriggerTick()
        {
            OnTick_?.Invoke();

            if (RepeatCount_ > 0)
            {
                RepeatCount_--;

                if (RepeatCount_ == 0)
                {
                    TriggerComplete();
                }
            }
        }

        private void TriggerComplete()
        {
            OnComplete_?.Invoke();
        }
    }
}