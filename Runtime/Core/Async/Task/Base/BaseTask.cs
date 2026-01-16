using System.Collections;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public abstract class BaseTask : BaseObject, ITask
    {
        public override string DebugName => GetType().Name;
        
        public float Progress { get; protected set; }
        public bool IsDone => State is TaskState.Completed or TaskState.Aborted;
        public object Result { get; protected set; }
        public TaskState State { get; protected set; }
        public TaskPriority Priority { get; private set; } = TaskPriority.Normal;

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
            : base()
        {
            State = TaskState.Pending;
        }

        public abstract void Dispose();

        public void SetPriority(TaskPriority priority)
        {
            Priority = priority;
        }
        
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

        protected void Complete(object result)
        {
            if (!IsDone)
            {
                Progress = 1f;
                State = TaskState.Completed;
                Result = result;
                _tcs?.TrySetResult(result);
            }
        }

        protected void Abort()
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

        bool IEnumerator.MoveNext()
        {
            return !IsDone;
        }
        
        void IEnumerator.Reset()
        {
        }

        object IEnumerator.Current => null;
    }
}