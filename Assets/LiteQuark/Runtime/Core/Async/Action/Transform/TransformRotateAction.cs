using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformRotateAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal_ ? "Local" : "World")}Rotate>({TS_.name},{OriginRotation_}->{TargetRotation_},{TotalTime_},{EaseKind_})";

        private readonly Quaternion Rotation_;
        private readonly float TotalTime_;
        private readonly bool IsLocal_;
        private readonly EaseKind EaseKind_;
        
        private Quaternion OriginRotation_;
        private Quaternion TargetRotation_;
        private float CurrentTime_;

        public TransformRotateAction(Transform transform, Quaternion rotation, float time, bool isLocal = true, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            Rotation_ = rotation;
            TotalTime_ = MathUtils.ClampMinTime(time);
            IsLocal_ = isLocal;
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            CurrentTime_ = 0;
            OriginRotation_ = TS_.localRotation;
            TargetRotation_ = Rotation_;
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            if (!CheckSafety())
            {
                return;
            }
            
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

    public class TransformRotateAroundAction : TransformBaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal_ ? "Local" : "World")}RotateAround>({TS_.name},{Center_},{Axis_},{TotalAngle_},{TotalTime_})";

        private readonly Vector3 Center_;
        private readonly Vector3 Axis_;
        private readonly float TotalAngle_;
        private readonly float TotalTime_;
        protected readonly bool IsLocal_;

        private float AnglePerSecond_;
        private float AccumulateAngle_;
        private float CurrentTime_;

        public TransformRotateAroundAction(Transform transform, Vector3 center, Vector3 axis, float angle, float time, bool isLocal = true)
            : base(transform)
        {
            Center_ = center;
            Axis_ = axis;
            TotalAngle_ = angle;
            TotalTime_ = MathUtils.ClampMinTime(time);
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
            if (!CheckSafety())
            {
                return;
            }
            
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
            if (!CheckSafety())
            {
                return;
            }
            
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

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformRotate(this ActionBuilder builder, Transform transform, Quaternion rotation, float time, bool isLocal = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformRotateAction(transform, rotation, time, isLocal, easeKind));
            return builder;
        }

        public static ActionBuilder TransformLocalRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, float angle, float time)
        {
            builder.Add(new TransformRotateAroundAction(transform, center, axis, angle, time, true));
            return builder;
        }

        public static ActionBuilder TransformWorldRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, float angle, float time)
        {
            builder.Add(new TransformRotateAroundAction(transform, center, axis, angle, time, false));
            return builder;
        }

        public static ActionBuilder TransformLocalTargetRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time)
        {
            builder.Add(new TransformTargetRotateAroundAction(transform, center, axis, targetPosition, angle, time, true));
            return builder;
        }

        public static ActionBuilder TransformWorldTargetRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time)
        {
            builder.Add(new TransformTargetRotateAroundAction(transform, center, axis, targetPosition, angle, time, false));
            return builder;
        }
    }
}