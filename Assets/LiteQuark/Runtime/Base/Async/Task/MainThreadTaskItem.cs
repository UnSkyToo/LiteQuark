using System;

namespace LiteQuark.Runtime
{
    public class MainThreadTaskEntity
    {
        private readonly Action<object> Func_;
        private readonly object Param_;

        public MainThreadTaskEntity(Action<object> func, object param)
        {
            Func_ = func;
            Param_ = param;
        }

        public void Invoke()
        {
            Func_?.Invoke(Param_);
        }
    }
}