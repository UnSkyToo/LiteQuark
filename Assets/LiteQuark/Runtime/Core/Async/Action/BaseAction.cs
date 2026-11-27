namespace LiteQuark.Runtime
{
    public abstract class BaseAction : BaseObject, IAction
    {
        public ulong ID => UniqueID;
        public bool IsDone { get; protected set; } = false;
        public bool IsUnscaled { get; private set; } = false;
        public bool IsSafety { get; private set; } = false;

        protected BaseAction()
        {
        }

        protected internal virtual void Dispose()
        {
        }

        protected internal virtual void MarkAsSafe()
        {
            IsSafety = true;
        }

        public virtual void Cancel()
        {
            IsDone = true;
        }

        protected internal virtual void Tick(float deltaTime)
        {
        }

        protected internal virtual void Execute()
        {
        }

        public BaseAction SetUnscaled(bool isUnscaled)
        {
            IsUnscaled = isUnscaled;
            return this;
        }
    }
}
