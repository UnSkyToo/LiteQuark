using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class BaseMotion : BaseObject
    {
        public Transform Master { get; set; }
        public bool IsEnd { get; protected set; }

        protected BaseMotion()
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