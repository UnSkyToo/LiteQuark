using System;
using System.Collections;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class AsyncOperationTask : BaseTask
    {
        private readonly AsyncOperation _asyncOperation;
        private Action _callback;
        
        public AsyncOperationTask(AsyncOperation asyncOperation, Action callback)
            : base()
        {
            _asyncOperation = asyncOperation;
            _callback = callback;
        }
        
        public override void Dispose()
        {
            _callback = null;
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
                else */if (_asyncOperation is { isDone: false })
                {
                    Progress = _asyncOperation.progress;
                    yield return _asyncOperation;
                }
                else
                {
                    Complete(null);
                }
            }

            _callback?.Invoke();
        }
    }
}