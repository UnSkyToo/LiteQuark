using System;

namespace LiteQuark.Runtime
{
    public abstract class BaseTimer : BaseObject, ITimer
    {
        public ulong ID => UniqueID;
        public bool IsEnd => RepeatCount_ == 0;

        public override string DebugName => $"Timer<{Interval_} - {RepeatCount_}>";

        protected readonly float Interval_;
        protected readonly Action OnTick_;
        protected readonly Action OnComplete_;

        protected bool IsPaused_;
        protected int RepeatCount_;
        protected float Time_;
        
        protected BaseTimer(float interval, int repeatCount, Action onTick, Action onComplete)
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