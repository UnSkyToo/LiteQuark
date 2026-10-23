using System;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public abstract class BaseTask : BaseObject, ITask
    {
        public override string DebugName => GetType().Name;

        public float Progress { get; protected set; }
        public bool IsExecuted { get; private set; }

        public TaskState State { get; private set; }
        public object Result { get; private set; }
        public Exception Error { get; private set; }

        public bool IsDone => State is TaskState.Completed or TaskState.Failed or TaskState.Cancelled;

        public UniTask<object> Task
        {
            get
            {
                if (_tcs == null)
                {
                    _tcs = new UniTaskCompletionSource<object>();
                    if (IsDone)
                    {
                        PublishResult();
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

        public void Execute()
        {
            if (State != TaskState.Pending)
            {
                return;
            }

            IsExecuted = true;
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

        protected bool Complete(object result)
        {
            if (State != TaskState.InProgress)
            {
                return false;
            }

            return TryTerminate(TaskState.Completed, result, null);
        }

        public void Fail(Exception error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            TryTerminate(TaskState.Failed, null, error);
        }

        public void Cancel()
        {
            TryTerminate(TaskState.Cancelled, null, null);
        }

        private bool TryTerminate(TaskState state, object result, Exception error)
        {
            if (IsDone)
            {
                return false;
            }

            Result = result;
            Error = error;

            if (state == TaskState.Completed)
            {
                Progress = 1f;
            }

            State = state;

            try
            {
                OnTerminated();
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Task {0} termination failed", DebugName);
            }
            finally
            {
                PublishResult();
            }

            return true;
        }

        private void PublishResult()
        {
            _tcs?.TrySetResult(Result);
        }

        protected abstract void OnExecute();

        protected virtual void OnTerminated()
        {
        }

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
