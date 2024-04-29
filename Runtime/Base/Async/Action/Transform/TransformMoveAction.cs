using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformMoveAction : BaseAction
    {
        public override string DebugName => $"<Transform{(IsLocal_ ? "Local" : "World")}Move>({TS_.name},{OriginPos_}->{TargetPos_},{TotalTime_},{EaseKind_})";

        private readonly Transform TS_;
        private readonly Vector3 Position_;
        private readonly float TotalTime_;
        private readonly bool IsLocal_;
        private readonly bool IsRelative_;
        private readonly EaseKind EaseKind_;
        
        private Vector3 OriginPos_;
        private Vector3 TargetPos_;
        private float CurrentTime_;

        public TransformMoveAction(Transform transform, Vector3 position, float time, bool isLocal = true, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            TS_ = transform;
            Position_ = position;
            TotalTime_ = Mathf.Max(time, 0.01f);
            IsLocal_ = isLocal;
            IsRelative_ = isRelative;
            EaseKind_ = easeKind;
        }

        public override void Execute()
        {
            CurrentTime_ = 0;
            OriginPos_ = TS_.localPosition;
            TargetPos_ = IsRelative_ ? OriginPos_ + Position_ : Position_;
            IsEnd = false;
        }

        public override void Tick(float deltaTime)
        {
            CurrentTime_ += deltaTime;
            var step = Mathf.Clamp01(CurrentTime_ / TotalTime_);
            var v = EaseUtils.Sample(EaseKind_, step);
            
            SetValue(Vector3.LerpUnclamped(OriginPos_, TargetPos_, v));

            if (step >= 1)
            {
                SetValue(TargetPos_);
                IsEnd = true;
            }
        }
        
        private void SetValue(Vector3 value)
        {
            if (IsLocal_)
            {
                TS_.localPosition = value;
            }
            else
            {
                TS_.position = value;
            }
        }
    }
}