using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class MotionBase : ObjectBase
    {
        public Transform Master { get; set; }
        public bool IsEnd { get; protected set; }

        protected MotionBase()
            : base()
        {
            IsEnd = true;
        }

        public void Stop()
        {
            IsEnd = true;
        }

        public virtual void Enter()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }
    }
}