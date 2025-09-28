using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public sealed class CoroutineTask : BaseTask
    {
        private readonly IEnumerator _item;
        private Action _callback;

        public CoroutineTask(IEnumerator item, Action callback)
            : base()
        {
            _item = item;
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
                else */if (_item != null && _item.MoveNext())
                {
                    yield return _item.Current;
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