using UnityEngine;

namespace LiteQuark.Runtime
{
    public class MotionMove : MotionBase
    {
        private readonly bool IsRelative_;
        private readonly float TotalTime_;
        private float CurrentTime_;
        private Vector3 BeginPosition_;
        private Vector3 EndPosition_;
        private Vector3 TargetPosition_;

        public MotionMove(float time, Vector3 position, bool isRelative)
            : base()
        {
            IsRelative_ = isRelative;
            TotalTime_ = time;
            BeginPosition_ = position;
            EndPosition_ = position;
        }

        public override void Enter()
        {
            IsEnd = false;
            CurrentTime_ = 0;
            BeginPosition_ = Master.localPosition;
            TargetPosition_ = EndPosition_;

            if (IsRelative_)
            {
                TargetPosition_ += BeginPosition_;
            }
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var t = CurrentTime_ / TotalTime_;
            if (t >= 1.0f)
            {
                t = 1.0f;
                IsEnd = true;
            }

            Master.localPosition = Vector3.Lerp(BeginPosition_, TargetPosition_, t);
        }
    }

    public class BezierMove : MotionBase
    {
        private readonly bool IsRelative_;
        private readonly float TotalTime_;
        private readonly IBezierCurve BezierCurve_;
        private float CurrentTime_;
        private Vector3 BasePosition_;

        public BezierMove(float time, IBezierCurve bezierCurve, bool isRelative)
            : base()
        {
            IsRelative_ = isRelative;
            TotalTime_ = time;
            BezierCurve_ = bezierCurve;
        }

        public override void Enter()
        {
            IsEnd = false;
            CurrentTime_ = 0;
            BasePosition_ = IsRelative_ ? Master.localPosition : Vector3.zero;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var t = CurrentTime_ / TotalTime_;
            if (t >= 1.0f)
            {
                t = 1.0f;
                IsEnd = true;
            }

            Master.localPosition = BasePosition_ + BezierCurve_.Lerp(t);
        }
    }
}