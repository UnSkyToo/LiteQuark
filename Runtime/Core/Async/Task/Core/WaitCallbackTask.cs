using System;

namespace LiteQuark.Runtime
{
    public sealed class WaitCallbackTask : BaseTask
    {
        private readonly Action<Action<bool>> _func;
        
        public WaitCallbackTask(Action<Action<bool>> func)
            : base()
        {
            _func = func;
        }
        
        public override void Dispose()
        {
        }

        protected override void OnExecute()
        {
            LiteUtils.SafeInvoke(_func, OnCallback);
        }

        private void OnCallback(bool isCompleted)
        {
            if (isCompleted)
            {
                Complete(true);
            }
            else
            {
                Abort();
            }
        }
    }
}