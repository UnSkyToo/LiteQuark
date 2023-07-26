using UnityEngine;

namespace LiteQuark.Runtime
{
    public class MotionRotate : MotionBase
    {
        private readonly float TotalTime_;
        private float CurrentTime_;
        private Quaternion BeginRotate_;
        private Quaternion EndRotate_;

        public MotionRotate(float time, Quaternion rotate)
            : base()
        {
            TotalTime_ = time;
            BeginRotate_ = rotate;
            EndRotate_ = rotate;
        }

        public override void Enter()
        {
            IsEnd = false;
            CurrentTime_ = 0;
            BeginRotate_ = Master.localRotation;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var T = CurrentTime_ / TotalTime_;
            if (T >= 1.0f)
            {
                Master.localRotation = EndRotate_;
                IsEnd = true;
            }

            Master.localRotation = Quaternion.Lerp(BeginRotate_, EndRotate_, T);
        }
    }
}