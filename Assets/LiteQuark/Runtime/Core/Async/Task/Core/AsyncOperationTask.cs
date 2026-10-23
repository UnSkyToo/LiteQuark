using System;
using System.Collections;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class AsyncOperationTask : BaseCoroutineTask
    {
        private readonly AsyncOperation _asyncOperation;
        private Action _callback;

        public AsyncOperationTask(AsyncOperation asyncOperation, Action callback)
            : base()
        {
            _asyncOperation = asyncOperation;
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
                if (_asyncOperation is { isDone: false })
                {
                    Progress = _asyncOperation.progress;
                    yield return _asyncOperation;
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
