using System;

namespace LiteQuark.Runtime
{
    public class MotionWaitTime : BaseMotion
    {
        private readonly float TotalTime_;
        private float CurrentTime_;

        public MotionWaitTime(float time)
            : base()
        {
            TotalTime_ = time;
        }

        public override void Enter()
        {
            CurrentTime_ = TotalTime_;
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ -= deltaTime;

            if (CurrentTime_ <= 0.0f)
            {
                IsEnd = true;
            }
        }
    }

    public class MotionWaitConditional : BaseMotion
    {
        private readonly Func<bool> ConditionFunc_;

        public MotionWaitConditional(Func<bool> conditionFunc)
            : base()
        {
            ConditionFunc_ = conditionFunc;
        }

        public override void Enter()
        {
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            IsEnd = ConditionFunc_?.Invoke() ?? true;
        }
    }
}