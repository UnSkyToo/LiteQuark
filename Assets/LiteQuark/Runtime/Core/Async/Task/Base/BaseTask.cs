using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public abstract class BaseTask : ITask
    {
        public float Progress { get; protected set; }
        public bool IsDone => State is TaskState.Completed or TaskState.Aborted;
        public object Result { get; protected set; }
        public TaskState State { get; protected set; }

        public UniTask<object> Task
        {
            get
            {
                if (_tcs == null)
                {
                    _tcs = new UniTaskCompletionSource<object>();
                    if (IsDone)
                    {
                        _tcs.TrySetResult(Result);
                    }
                }
                return _tcs.Task;
            }
        }

        private UniTaskCompletionSource<object> _tcs;

        protected BaseTask()
        {
            State = TaskState.Pending;
        }

        public abstract void Dispose();
        
        public void Execute()
        {
            if (State != TaskState.Pending)
            {
                return;
            }

            State = TaskState.InProgress;
            Progress = 0;
            Result = null;
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
                Result = result;
                _tcs?.TrySetResult(result);
            }
        }

        public void Abort()
        {
            if (!IsDone)
            {
                State = TaskState.Aborted;
                Result = null;
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