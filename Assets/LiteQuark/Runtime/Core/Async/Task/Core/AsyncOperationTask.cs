using System;
using System.Collections;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class AsyncOperationTask : BaseTask
    {
        private readonly AsyncOperation AsyncOperation_;
        private Action Callback_;
        
        public AsyncOperationTask(AsyncOperation asyncOperation, Action callback)
            : base()
        {
            AsyncOperation_ = asyncOperation;
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
                else */if (AsyncOperation_ is { isDone: false })
                {
                    Progress = AsyncOperation_.progress;
                    yield return AsyncOperation_;
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