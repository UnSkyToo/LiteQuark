using System;

namespace LiteQuark.Runtime
{
    public sealed class ParallelTask : BaseTask
    {
        private readonly ITask[] _subTasks;
        private readonly Action<bool> _callback;
        
        public ParallelTask(ITask[] subTasks, Action<bool> callback)
            : base()
        {
            _subTasks = subTasks ?? Array.Empty<ITask>();
            _callback = callback;
        }
        
        public override void Dispose()
        {
            foreach (var subTask in _subTasks)
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
                _callback?.Invoke(isAllCompleted);
            }
        }

        private bool CheckTaskState()
        {
            foreach (var task in _subTasks)
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
            foreach (var task in _subTasks)
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