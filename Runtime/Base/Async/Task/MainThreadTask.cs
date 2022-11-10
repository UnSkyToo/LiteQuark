using System;

namespace LiteQuark.Runtime
{
    public sealed class MainThreadTask : ITask
    {
        public bool IsEnd { get; private set; }
        public bool IsExecute { get; private set; }
        
        private readonly Action<object> Func_;
        private readonly object Param_;

        public MainThreadTask(Action<object> func, object param)
        {
            IsEnd = false;
            IsExecute = false;
            
            Func_ = func;
            Param_ = param;
        }
        
        public void Dispose()
        {
        }

        public void Execute()
        {
            if (IsExecute || IsEnd)
            {
                return;
            }

            IsEnd = true;
            IsExecute = true;
            Func_?.Invoke(Param_);
        }
    }
}