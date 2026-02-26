using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public sealed class AsyncResultTask : BaseCoroutineTask
    {
        private readonly IAsyncResult _asyncResult;
        private Action _callback;

        public AsyncResultTask(IAsyncResult asyncResult, Action callback)
            : base()
        {
            _asyncResult = asyncResult;
            _callback = callback;
        }

        public override void Dispose()
        {
            _callback = null;
        }
        
        protected override IEnumerator ExecuteInternal()
        {
            while (State == TaskState.InProgress)
            {
                /*if (IsPause)
                {
                    yield return null;
                }
                else */if (_asyncResult is { IsCompleted: false })
                {
                    yield return null;
                }
                else
                {
                    Complete(null);
                }
            }
            
            LiteUtils.SafeInvoke(_callback);
        }
    }
}