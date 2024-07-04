using UnityEngine;

namespace LiteQuark.Runtime
{
    public class RectTransformMoveAction : RectTransformBaseAction
    {
        public override string DebugName => $"<RectTransformMove>({RT_.name},{OriginPos_}->{TargetPos_},{TotalTime_},{EaseKind_})";

        private readonly Vector2 Position_;
        private readonly float TotalTime_;
        private readonly bool IsRelative_;
        private readonly EaseKind EaseKind_;
        
        private Vector2 OriginPos_;
        private Vector2 TargetPos_;
        private float CurrentTime_;

        public RectTransformMoveAction(RectTransform transform, Vector2 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
            : base(transform)
        {
            Position_ = position;
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
            OriginPos_ = GetValue();
            TargetPos_ = IsRelative_ ? OriginPos_ + Position_ : Position_;
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
            
            SetValue(Vector2.LerpUnclamped(OriginPos_, TargetPos_, v));

            if (step >= 1)
            {
                SetValue(TargetPos_);
                IsEnd = true;
            }
        }

        private Vector2 GetValue()
        {
            return RT_.anchoredPosition;
        }
        
        private void SetValue(Vector2 value)
        {
            RT_.anchoredPosition = value;
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder RectTransformMove(this ActionBuilder builder, RectTransform transform, Vector2 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new RectTransformMoveAction(transform, position, time, isRelative, easeKind));
            return builder;
        }
    }
}