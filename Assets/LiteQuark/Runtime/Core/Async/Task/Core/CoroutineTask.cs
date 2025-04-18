using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public sealed class CoroutineTask : BaseTask
    {
        private readonly IEnumerator Item_;
        private Action Callback_;

        public CoroutineTask(IEnumerator item, Action callback)
            : base()
        {
            Item_ = item;
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
                else */if (Item_ != null && Item_.MoveNext())
                {
                    yield return Item_.Current;
                }
                else
                {
                    Complete(null);
                }
            }

            Callback_?.Invoke();
        }
    }
}