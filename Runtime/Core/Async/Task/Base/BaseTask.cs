namespace LiteQuark.Runtime
{
    public abstract class BaseTask : ITask
    {
        public TaskState State { get; protected set; }

        protected BaseTask()
        {
            State = TaskState.Waiting;
        }

        public abstract void Dispose();
        
        public void Execute()
        {
            if (State != TaskState.Waiting)
            {
                return;
            }

            State = TaskState.InProgress;
            OnExecute();
        }

        public void Tick(float deltaTime)
        {
            if (State != TaskState.InProgress)
            {
                return;
            }
            
            OnTick(deltaTime);
        }

        public void Complete()
        {
            State = TaskState.Completed;
        }

        public void Abort()
        {
            State = TaskState.Aborted;
        }

        protected abstract void OnExecute();

        protected virtual void OnTick(float deltaTime)
        {
        }
    }
}