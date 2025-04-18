using System;

namespace LiteQuark.Runtime
{
    public sealed class ParallelTask : BaseTask
    {
        private readonly ITask[] SubTasks_;
        private readonly Action<bool> Callback_;
        
        public ParallelTask(ITask[] subTasks, Action<bool> callback)
            : base()
        {
            SubTasks_ = subTasks ?? Array.Empty<ITask>();
            Callback_ = callback;
        }
        
        public override void Dispose()
        {
            foreach (var subTask in SubTasks_)
            {
                subTask.Dispose();
            }
        }

        protected override void OnExecute()
        {
        }

        protected override void OnTick(float deltaTime)
        {
            if (CheckTaskState())
            {
                var isAllCompleted = IsAllCompleted();
                Complete(isAllCompleted);
                Callback_?.Invoke(isAllCompleted);
            }
        }

        private bool CheckTaskState()
        {
            foreach (var task in SubTasks_)
            {
                if (!task.IsDone)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsAllCompleted()
        {
            foreach (var task in SubTasks_)
            {
                if (task.State == TaskState.Aborted)
                {
                    return false;
                }
            }

            return true;
        }
    }
}