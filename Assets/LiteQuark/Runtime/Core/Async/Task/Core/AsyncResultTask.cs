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

        protected override void OnDispose()
        {
            _callback = null;
        }

        protected override IEnumerator ExecuteInternal()
        {
            while (State == TaskState.Running)
            {
                if (_asyncResult is { IsCompleted: false })
                {
                    yield return null;
                }
                else
                {
                    Complete(null);
                }
            }

            if (State == TaskState.Succeeded)
            {
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback);
            }
        }
    }
}
