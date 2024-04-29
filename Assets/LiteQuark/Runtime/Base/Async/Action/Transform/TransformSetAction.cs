using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformSetPositionAction : BaseAction
    {
        public override string DebugName => $"<TransformSet{(IsLocal_ ? "Local" : "World")}Position>({TS_.name},{Position_})";

        private readonly Transform TS_;
        private readonly Vector3 Position_;
        private readonly bool IsLocal_;

        public TransformSetPositionAction(Transform transform, Vector3 position, bool isLocal = true)
        {
            TS_ = transform;
            Position_ = position;
            IsLocal_ = isLocal;
        }

        public override void Execute()
        {
            if (IsLocal_)
            {
                TS_.localPosition = Position_;
            }
            else
            {
                TS_.position = Position_;
            }

            IsEnd = false;
        }
    }
    
    public class TransformSetScaleAction : BaseAction
    {
        public override string DebugName => $"<TransformSetScale>({TS_.name},{Scale_})";

        private readonly Transform TS_;
        private readonly Vector3 Scale_;

        public TransformSetScaleAction(Transform transform, Vector3 scale)
        {
            TS_ = transform;
            Scale_ = scale;
        }

        public override void Execute()
        {
            TS_.localScale = Scale_;
            IsEnd = false;
        }
    }
    
    public class TransformSetRotationAction : BaseAction
    {
        public override string DebugName => $"<TransformSet{(IsLocal_ ? "Local" : "World")}Rotation>({TS_.name},{Rotation_})";

        private readonly Transform TS_;
        private readonly Quaternion Rotation_;
        private readonly bool IsLocal_;

        public TransformSetRotationAction(Transform transform, Quaternion rotation, bool isLocal = true)
        {
            TS_ = transform;
            Rotation_ = rotation;
            IsLocal_ = isLocal;
        }

        public override void Execute()
        {
            if (IsLocal_)
            {
                TS_.localRotation = Rotation_;
            }
            else
            {
                TS_.rotation = Rotation_;
            }

            IsEnd = false;
        }
    }
    
    public class TransformSetAlphaAction : BaseAction
    {
        public override string DebugName => $"<TransformSetAlpha>({TS_.name},{Alpha_})";

        private readonly Transform TS_;
        private readonly AlphaBox AlphaBox_;
        private readonly float Alpha_;

        public TransformSetAlphaAction(Transform transform, float alpha)
        {
            TS_ = transform;
            AlphaBox_ = new AlphaBox(transform);
            Alpha_ = alpha;
        }

        public override void Execute()
        {
            AlphaBox_.SetAlpha(Alpha_);
            IsEnd = false;
        }
    }
    
    public class TransformSetActiveAction : BaseAction
    {
        public override string DebugName => $"<TransformSetActive>({TS_.name},{Value_})";

        private readonly Transform TS_;
        private readonly bool Value_;

        public TransformSetActiveAction(Transform transform, bool value)
        {
            TS_ = transform;
            Value_ = value;
        }

        public override void Execute()
        {
            if (TS_ != null)
            {
                TS_.gameObject.SetActive(Value_);
            }
            IsEnd = false;
        }
    }
}