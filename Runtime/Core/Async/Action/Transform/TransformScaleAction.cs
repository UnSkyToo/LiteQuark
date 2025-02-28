using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformScaleAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformScale>({TS_.name},{OriginScale_}->{TargetScale_},{TotalTime_},{EaseKind_})";
        
        private readonly Vector3 Scale_;
        private readonly float TotalTime_;
        private readonly bool IsRelative_;
        private readonly EaseKind EaseKind_;
        
        private Vector3 OriginScale_;
        private Vector3 TargetScale_;
        private float CurrentTime_;

        public TransformScaleAction(Transform transform, Vector3 scale, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            Scale_ = scale;
            TotalTime_ = MathUtils.ClampMinTime(time);
            IsRelative_ = isRelative;
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            CurrentTime_ = 0;
            OriginScale_ = TS_.localScale;
            TargetScale_ = IsRelative_ ? OriginScale_ + Scale_ : Scale_;
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
            
            TS_.localScale = Vector3.LerpUnclamped(OriginScale_, TargetScale_, v);

            if (step >= 1)
            {
                TS_.localScale = TargetScale_;
                IsEnd = true;
            }
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformScale(this ActionBuilder builder, Transform transform, Vector3 scale, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformScaleAction(transform, scale, time, isRelative, easeKind));
            return builder;
        }
    }
}