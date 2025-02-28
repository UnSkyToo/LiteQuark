namespace LiteQuark.Runtime
{
    public abstract class BaseAction : BaseObject, IAction
    {
        public ulong ID => UniqueID;
        public bool IsEnd { get; protected set; }
        public bool IsSafety { get; private set; } = false;
        public System.Action<IAction> FinalCallback { get; private set; } = null;
        
        protected BaseAction()
        {
        }
        
        public virtual void Dispose()
        {
        }

        public virtual void MarkSafety()
        {
            IsSafety = true;
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

        public IAction SetFinalCallback(System.Action<IAction> callback)
        {
            FinalCallback = callback;
            return this;
        }
    }
}