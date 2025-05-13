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
                if (_tcs == null)
                {
                    _tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
                    if (IsDone)
                    {
                        _tcs.SetResult(null);
                    }
                }
                return _tcs.Task;
            }
        }

        private System.Threading.Tasks.TaskCompletionSource<object> _tcs;

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
            if (State != TaskState.InProgress)
            {
                return;
            }

            OnTick(deltaTime);
        }

        public void Complete(object result)
        {
            if (!IsDone)
            {
                Progress = 1f;
                State = TaskState.Completed;
                _tcs?.TrySetResult(result);
            }
        }

        public void Abort()
        {
            if (!IsDone)
            {
                State = TaskState.Aborted;
                _tcs?.TrySetResult(null);
            }
        }

        public virtual void Cancel()
        {
            Abort();
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