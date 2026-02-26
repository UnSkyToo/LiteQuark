using System.Collections;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class BaseCoroutineTask : BaseTask
    {
        private Coroutine _coroutine;
        
        protected BaseCoroutineTask()
            : base()
        {
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

        protected abstract IEnumerator ExecuteInternal();
    }
}