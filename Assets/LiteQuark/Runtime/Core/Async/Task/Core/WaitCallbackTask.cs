using System;

namespace LiteQuark.Runtime
{
    public sealed class WaitCallbackTask : BaseTask
    {
        private readonly Action<Action<bool>> _func;

        public WaitCallbackTask(Action<Action<bool>> func)
            : base()
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        protected override void OnDispose()
        {
        }

        protected override void OnExecute()
        {
            _func(OnCallback);
        }

        private void OnCallback(bool success)
        {
            if (IsDone)
            {
                return;
            }

            if (success)
            {
                Complete(true);
            }
            else
            {
                Fail(new InvalidOperationException("The callback reported task failure."));
            }
        }
    }
}
