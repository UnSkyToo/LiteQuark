using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformScaleAction : BaseAction
    {
        public override string DebugName => $"<TransformScale>({TS_.name},{OriginScale_}->{TargetScale_},{TotalTime_},{EaseKind_})";

        private readonly Transform TS_;
        private readonly Vector3 Scale_;
        private readonly float TotalTime_;
        private readonly bool IsRelative_;
        private readonly EaseKind EaseKind_;
        
        private Vector3 OriginScale_;
        private Vector3 TargetScale_;
        private float CurrentTime_;

        public TransformScaleAction(Transform transform, Vector3 scale, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            TS_ = transform;
            Scale_ = scale;
            TotalTime_ = Mathf.Max(time, 0.01f);
            IsRelative_ = isRelative;
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            CurrentTime_ = 0;
            OriginScale_ = TS_.localScale;
            TargetScale_ = IsRelative_ ? OriginScale_ + Scale_ : OriginScale_;
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
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
}