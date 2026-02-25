using System;
using System.Collections;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class AsyncOperationTask : BaseTask
    {
        private readonly AsyncOperation _asyncOperation;
        private Coroutine _coroutine;
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
        
        public override void Cancel()
        {
            base.Cancel();
            
            if (_coroutine != null)
            {
                LiteRuntime.Task.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        protected override void OnExecute()
        {
            _coroutine = LiteRuntime.Task.StartCoroutine(ExecuteInternal());
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
            
            LiteUtils.SafeInvoke(_callback);
        }
    }
}