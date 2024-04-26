using UnityEngine;

namespace LiteQuark.Runtime
{
    public class MotionSetPosition : BaseMotion
    {
        private readonly Vector3 Position_;

        public MotionSetPosition(Vector3 position)
            : base()
        {
            Position_ = position;
        }

        public override void Enter()
        {
            Master.localPosition = Position_;
        }
    }

    public class MotionSetRotation : BaseMotion
    {
        private readonly Quaternion Rotation_;

        public MotionSetRotation(Quaternion rotation)
            : base()
        {
            Rotation_ = rotation;
        }

        public override void Enter()
        {
            Master.localRotation = Rotation_;
        }
    }

    public class MotionSetScale : BaseMotion
    {
        private readonly Vector3 Scale_;

        public MotionSetScale(Vector3 scale)
            : base()
        {
            Scale_ = scale;
        }

        public override void Enter()
        {
            Master.localScale = Scale_;
        }
    }

    public class MotionSetAlpha : BaseMotion
    {
        private readonly float Alpha_;
        private MotionAlphaBox AlphaBox_;

        public MotionSetAlpha(float alpha)
            : base()
        {
            Alpha_ = alpha;
        }

        public override void Enter()
        {
            AlphaBox_ = new MotionAlphaBox(Master);
            AlphaBox_.SetAlpha(Alpha_);
        }
    }

    public class MotionSetActive : BaseMotion
    {
        private readonly bool Value_;

        public MotionSetActive(bool value)
            : base()
        {
            Value_ = value;
        }

        public override void Enter()
        {
            if (Master != null)
            {
                Master.gameObject.SetActive(Value_);
            }
        }
    }
}