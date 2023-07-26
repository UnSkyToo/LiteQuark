using UnityEngine;

namespace LiteQuark.Runtime
{
    public class MotionScale : MotionBase
    {
        private readonly bool IsRelative_;
        private readonly float TotalTime_;
        private float CurrentTime_;
        private Vector3 BeginScale_;
        private Vector3 EndScale_;
        private Vector3 TargetScale_;

        public MotionScale(float time, Vector3 scale, bool isRelative)
            : base()
        {
            IsRelative_ = isRelative;
            TotalTime_ = time;
            BeginScale_ = scale;
            EndScale_ = scale;
        }

        public override void Enter()
        {
            IsEnd = false;
            CurrentTime_ = 0;
            BeginScale_ = Master.localScale;
            TargetScale_ = EndScale_;

            if (IsRelative_)
            {
                TargetScale_ += BeginScale_;
            }
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var T = CurrentTime_ / TotalTime_;
            if (T >= 1.0f)
            {
                Master.localScale = TargetScale_;
                IsEnd = true;
            }

            Master.localScale = Vector3.Lerp(BeginScale_, TargetScale_, T);
        }
    }
}