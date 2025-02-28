using System;
using System.Collections;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class AsyncOperationTask : BaseTask
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
            LiteRuntime.Task.MonoBehaviourInstance.StartCoroutine(ExecuteInternal());
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
                    yield return AsyncOperation_;
                }
                else
                {
                    Stop();
                }
            }

            Callback_?.Invoke();
        }
    }
}