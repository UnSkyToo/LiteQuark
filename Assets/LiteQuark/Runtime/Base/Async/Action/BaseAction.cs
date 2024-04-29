﻿namespace LiteQuark.Runtime
{
    public abstract class BaseAction : BaseObject, IAction
    {
        public ulong ID => UniqueID;
        public bool IsEnd { get; protected set; }

        protected BaseAction()
        {
        }
        
        public virtual void Dispose()
        {
        }

        public virtual void Stop()
        {
            IsEnd = true;
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Execute()
        {
        }
    }
}