using System;

namespace LiteQuark.Runtime
{
    public sealed class MainThreadTask : BaseTask
    {
        private readonly Action<object> Func_;
        private readonly object Param_;

        public MainThreadTask(Action<object> func, object param)
            : base()
        {
            Func_ = func;
            Param_ = param;
        }

        public override void Dispose()
        {
        }

        protected override void OnExecute()
        {
            Func_?.Invoke(Param_);
            Complete(null);
        }
    }
}