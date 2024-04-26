namespace LiteQuark.Runtime
{
    public abstract class BaseTask : ITask
    {
        public bool IsEnd { get; private set; }
        public bool IsExecute { get; private set; }

        protected BaseTask()
        {
            IsEnd = false;
            IsExecute = false;
        }

        public abstract void Dispose();
        
        public void Execute()
        {
            if (IsExecute || IsEnd)
            {
                return;
            }

            IsExecute = true;
            OnExecute();
        }

        public void Stop()
        {
            IsEnd = true;
        }

        protected abstract void OnExecute();
    }
}