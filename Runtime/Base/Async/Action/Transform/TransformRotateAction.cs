using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformRotateAction : BaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal_ ? "Local" : "World")}Rotate>({TS_.name},{OriginRotation_}->{TargetRotation_},{TotalTime_},{EaseKind_})";

        private readonly Transform TS_;
        private readonly Quaternion Rotation_;
        private readonly float TotalTime_;
        private readonly bool IsLocal_;
        private readonly EaseKind EaseKind_;
        
        private Quaternion OriginRotation_;
        private Quaternion TargetRotation_;
        private float CurrentTime_;

        public TransformRotateAction(Transform transform, Quaternion rotation, float time, bool isLocal = true, EaseKind easeKind = EaseKind.Linear)
        {
            TS_ = transform;
            Rotation_ = rotation;
            TotalTime_ = Mathf.Max(time, 0.01f);
            IsLocal_ = isLocal;
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            CurrentTime_ = 0;
            OriginRotation_ = TS_.localRotation;
            TargetRotation_ = Rotation_;
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var step = Mathf.Clamp01(CurrentTime_ / TotalTime_);
            var v = EaseUtils.Sample(EaseKind_, step);
            
            SetValue(Quaternion.LerpUnclamped(OriginRotation_, TargetRotation_, v));

            if (step >= 1)
            {
                SetValue(TargetRotation_);
                IsEnd = true;
            }
        }
        
        private void SetValue(Quaternion value)
        {
            if (IsLocal_)
            {
                TS_.localRotation = value;
            }
            else
            {
                TS_.rotation = value;
            }
        }
    }

    public class TransformRotateAroundAction : BaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal_ ? "Local" : "World")}RotateAround>({TS_.name},{Center_},{Axis_},{TotalAngle_},{TotalTime_})";

        protected readonly Transform TS_;
        private readonly Vector3 Center_;
        private readonly Vector3 Axis_;
        private readonly float TotalAngle_;
        private readonly float TotalTime_;
        protected readonly bool IsLocal_;

        private float AnglePerSecond_;
        private float AccumulateAngle_;
        private float CurrentTime_;

        public TransformRotateAroundAction(Transform transform, Vector3 center, Vector3 axis, float angle, float time, bool isLocal = true)
        {
            TS_ = transform;
            Center_ = center;
            Axis_ = axis;
            TotalAngle_ = angle;
            TotalTime_ = Mathf.Max(time, 0.01f);
            IsLocal_ = isLocal;
        }

        public override void Execute()
        {
            CurrentTime_ = 0;
            AnglePerSecond_ = TotalAngle_ / TotalTime_;
            AccumulateAngle_ = 0f;
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var angle = CalculateNextAngle(deltaTime);

            if (IsLocal_)
            {
                var vector3 = Quaternion.AngleAxis(angle, Axis_) * (TS_.localPosition - Center_);
                TS_.localPosition = Center_ + vector3;
            }
            else
            {
                TS_.RotateAround(Center_, Axis_, angle);
            }
            
            if (CurrentTime_ >= TotalTime_)
            {
                IsEnd = true;
            }
        }

        private float CalculateNextAngle(float deltaTime)
        {
            var angle = deltaTime * AnglePerSecond_;
            
            if (TotalAngle_ > 0 && AccumulateAngle_ + angle > TotalAngle_)
            {
                angle = TotalAngle_ - AccumulateAngle_;
                AccumulateAngle_ = TotalAngle_;
            }
            else if (TotalAngle_ < 0 && AccumulateAngle_ + angle < TotalAngle_)
            {
                angle = TotalAngle_ - AccumulateAngle_;
                AccumulateAngle_ = TotalAngle_;
            }
            else
            {
                AccumulateAngle_ += angle;
            }
            
            return angle;
        }
    }

    public class TransformTargetRotateAroundAction : TransformRotateAroundAction
    {
        private readonly Vector3 TargetPosition_;
        
        public TransformTargetRotateAroundAction(Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time, bool isLocal = true)
            : base(transform, center, axis, angle, time, isLocal)
        {
            TargetPosition_ = targetPosition;
        }

        public override void Tick(float deltaTime)
        {
            if (IsLocal_)
            {
                if (Vector3.Distance(TS_.localPosition, TargetPosition_) < 0.1f)
                {
                    TS_.localPosition = TargetPosition_;
                    IsEnd = true;
                }
            }
            else
            {
                if (Vector3.Distance(TS_.position, TargetPosition_) < 0.1f)
                {
                    TS_.position = TargetPosition_;
                    IsEnd = true;
                }
            }

            if (!IsEnd)
            {
                base.Tick(deltaTime);
            }
        }
    }
}