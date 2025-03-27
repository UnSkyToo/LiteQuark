namespace LiteQuark.Runtime
{
    public abstract class BaseTask : ITask
    {
        public float Progress { get; protected set; }
        public bool IsDone => State is TaskState.Completed or TaskState.Aborted;
        public TaskState State { get; protected set; }

        public System.Threading.Tasks.Task Task
        {
            get
            {
                if (TCS_ == null)
                {
                    TCS_ = new System.Threading.Tasks.TaskCompletionSource<object>();
                    if (IsDone)
                    {
                        TCS_.SetResult(null);
                    }
                }
                return TCS_.Task;
            }
        }

        private System.Threading.Tasks.TaskCompletionSource<object> TCS_;

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
            Progress = 0;
            OnExecute();
        }

        public void Tick(float deltaTime)
        {
            if (State == TaskState.Waiting)
            {
                Execute();
            }
            else if (State == TaskState.InProgress)
            {
                OnTick(deltaTime);
            }
        }

        public void Complete()
        {
            if (!IsDone)
            {
                Progress = 1f;
                State = TaskState.Completed;
                TCS_?.TrySetResult(null);
            }
        }

        public void Abort()
        {
            if (!IsDone)
            {
                State = TaskState.Aborted;
                TCS_?.TrySetResult(null);
            }
        }

        protected abstract void OnExecute();

        protected virtual void OnTick(float deltaTime)
        {
        }

        public bool MoveNext()
        {
            return !IsDone;
        }
        
        public void Reset()
        {
        }

        public object Current => null;
    }
}