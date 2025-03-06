using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public sealed class AsyncResultTask : BaseTask
    {
        private readonly IAsyncResult AsyncResult_;
        private Action Callback_;

        public AsyncResultTask(IAsyncResult asyncResult, Action callback)
            : base()
        {
            AsyncResult_ = asyncResult;
            Callback_ = callback;
        }

        public override void Dispose()
        {
            Callback_ = null;
        }

        protected override void OnExecute()
        {
            LiteRuntime.Task.StartCoroutine(ExecuteInternal());
        }
        
        private IEnumerator ExecuteInternal()
        {
            while (State == TaskState.InProgress)
            {
                /*if (IsPause)
                {
                    yield return null;
                }
                else */if (AsyncResult_ is { IsCompleted: false })
                {
                    yield return null;
                }
                else
                {
                    Complete();
                }
            }

            Callback_?.Invoke();
        }
    }
}