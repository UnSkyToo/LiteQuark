using System;

namespace LiteQuark.Runtime
{
    public sealed class MainThreadTask : ITask
    {
        public TaskState State { get; private set; }

        private readonly Action<object> Func_;
        private readonly object Param_;

        public MainThreadTask(Action<object> func, object param)
        {
            State = TaskState.Waiting;

            Func_ = func;
            Param_ = param;
        }

        public void Dispose()
        {
        }

        public void Execute()
        {
            if (State != TaskState.Waiting)
            {
                return;
            }

            State = TaskState.InProgress;
            Func_?.Invoke(Param_);
            State = TaskState.Completed;
        }

        public void Tick(float deltaTime)
        {
        }
    }
}