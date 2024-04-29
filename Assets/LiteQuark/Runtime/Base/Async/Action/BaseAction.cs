namespace LiteQuark.Runtime
{
    public abstract class BaseAction : BaseObject, IAction
    {
        public bool IsEnd { get; protected set; }

        protected BaseAction()
        {
        }
        
        public virtual void Dispose()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Execute()
        {
        }
    }
}