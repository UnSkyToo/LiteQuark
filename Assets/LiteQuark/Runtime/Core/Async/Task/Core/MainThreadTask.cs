using System;

namespace LiteQuark.Runtime
{
    public sealed class MainThreadTask : BaseTask
    {
        private readonly Action<object> _func;
        private readonly object _param;

        public MainThreadTask(Action<object> func, object param)
            : base()
        {
            _func = func;
            _param = param;
        }

        public override void Dispose()
        {
        }

        protected override void OnExecute()
        {
            _func?.Invoke(_param);
            Complete(null);
        }
    }
}