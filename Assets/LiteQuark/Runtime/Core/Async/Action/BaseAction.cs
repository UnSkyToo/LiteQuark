namespace LiteQuark.Runtime
{
    public abstract class BaseAction : BaseObject, IAction
    {
        public ulong ID => UniqueID;
        public bool IsDone { get; protected set; } = false;
        public bool IsUnscaled { get; private set; } = false;
        public bool IsSafety { get; private set; } = false;
        public System.Action<IAction> FinalCallback { get; private set; } = null;
        
        protected BaseAction()
        {
        }
        
        public virtual void Dispose()
        {
        }

        public virtual void MarkAsSafe()
        {
            IsSafety = true;
        }

        public virtual void Stop()
        {
            IsDone = true;
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Execute()
        {
        }

        public IAction SetFinalCallback(System.Action<IAction> callback)
        {
            FinalCallback = callback;
            return this;
        }
        
        public IAction SetUnscaled(bool isUnscaled)
        {
            IsUnscaled = isUnscaled;
            return this;
        }
    }
}