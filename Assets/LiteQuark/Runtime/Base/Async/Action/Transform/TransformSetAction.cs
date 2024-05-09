using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformSetPositionAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSet{(IsLocal_ ? "Local" : "World")}Position>({TS_.name},{Position_})";
        
        private readonly Vector3 Position_;
        private readonly bool IsLocal_;

        public TransformSetPositionAction(Transform transform, Vector3 position, bool isLocal = true)
            : base(transform)
        {
            Position_ = position;
            IsLocal_ = isLocal;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            if (IsLocal_)
            {
                TS_.localPosition = Position_;
            }
            else
            {
                TS_.position = Position_;
            }

            IsEnd = true;
        }
    }
    
    public class TransformSetScaleAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetScale>({TS_.name},{Scale_})";
        
        private readonly Vector3 Scale_;

        public TransformSetScaleAction(Transform transform, Vector3 scale)
            : base(transform)
        {
            Scale_ = scale;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            TS_.localScale = Scale_;
            IsEnd = true;
        }
    }
    
    public class TransformSetRotationAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSet{(IsLocal_ ? "Local" : "World")}Rotation>({TS_.name},{Rotation_})";
        
        private readonly Quaternion Rotation_;
        private readonly bool IsLocal_;

        public TransformSetRotationAction(Transform transform, Quaternion rotation, bool isLocal = true)
            : base(transform)
        {
            Rotation_ = rotation;
            IsLocal_ = isLocal;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            if (IsLocal_)
            {
                TS_.localRotation = Rotation_;
            }
            else
            {
                TS_.rotation = Rotation_;
            }

            IsEnd = true;
        }
    }
    
    public class TransformSetRotationAroundAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetRotationAround>({TS_.name},{Center_},{Axis_},{Angle_})";
        
        private readonly Vector3 Center_;
        private readonly Vector3 Axis_;
        private readonly float Angle_;

        public TransformSetRotationAroundAction(Transform transform, Vector3 center, Vector3 axis, float angle)
            : base(transform)
        {
            Center_ = center;
            Axis_ = axis;
            Angle_ = angle;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            TS_.RotateAround(Center_, Axis_, Angle_);
            IsEnd = true;
        }
    }
    
    public class TransformSetAlphaAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetAlpha>({TS_.name},{Alpha_})";
        
        private readonly IAlphaBox AlphaBox_;
        private readonly float Alpha_;

        public TransformSetAlphaAction(Transform transform, float alpha, IAlphaBox box = null)
            : base(transform)
        {
            AlphaBox_ = box ?? new AlphaBox(transform);
            Alpha_ = alpha;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            AlphaBox_.SetAlpha(Alpha_);
            IsEnd = true;
        }
    }
    
    public class TransformSetActiveAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetActive>({TS_.name},{Value_})";
        
        private readonly bool Value_;

        public TransformSetActiveAction(Transform transform, bool value)
            : base(transform)
        {
            Value_ = value;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            TS_.gameObject.SetActive(Value_);
            IsEnd = true;
        }
    }
}