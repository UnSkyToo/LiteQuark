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
}