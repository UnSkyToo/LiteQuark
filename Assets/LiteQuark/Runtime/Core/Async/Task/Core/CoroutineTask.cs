using System;
using System.Collections;

namespace LiteQuark.Runtime
{
    public sealed class CoroutineTask : BaseCoroutineTask
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

        protected override IEnumerator ExecuteInternal()
        {
            if (_item != null)
            {
                yield return _item;
            }

            if (Complete(null))
            {
                LiteUtils.SafeInvoke(_callback);
            }
        }
    }
}
