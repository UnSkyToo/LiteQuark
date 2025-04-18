using System;

namespace LiteQuark.Runtime
{
    public sealed class WaitCallbackTask : BaseTask
    {
        private readonly Action<Action<bool>> Func_;
        
        public WaitCallbackTask(Action<Action<bool>> func)
            : base()
        {
            Func_ = func;
        }
        
        public override void Dispose()
        {
        }

        protected override void OnExecute()
        {
            Func_.Invoke(OnCallback);
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