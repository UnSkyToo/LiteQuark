using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class PipelineTask : BaseTask
    {
        public event Action<int, int> Progress;
        public event Action<bool> Completed;
        
        private readonly List<ITask> SubTaskList_ = null;
        private readonly Action<bool> Callback_ = null;
        private ITask CurrentSubTask_ = null;
        private int Index_ = 0;
        
        public PipelineTask(ITask[] subTasks, Action<bool> callback)
            : base()
        {
            SubTaskList_ = new List<ITask>(subTasks);
            Callback_ = callback;
            State = TaskState.Waiting;
            Index_ = 0;
        }

        public override void Dispose()
        {
            foreach (var subTask in SubTaskList_)
            {
                subTask.Dispose();
            }
            SubTaskList_.Clear();
        }

        protected override void OnExecute()
        {
            State = TaskState.InProgress;
            Index_ = 0;
            NextTask();
        }

        protected override void OnTick(float deltaTime)
        {
            if (CurrentSubTask_ == null)
            {
                Complete();
                return;
            }
            
            if (CurrentSubTask_.State == TaskState.Completed)
            {
                NextTask();
            }
            else if (CurrentSubTask_.State == TaskState.Aborted)
            {
                MarkResult(false);
            }
        }

        private void NextTask()
        {
            Progress?.Invoke(Index_, SubTaskList_.Count);

            if (Index_ >= SubTaskList_.Count)
            {
                CurrentSubTask_ = null;
                MarkResult(true);
                return;
            }
            
            CurrentSubTask_ = SubTaskList_[Index_++];
            CurrentSubTask_?.Execute();
        }

        private void MarkResult(bool isCompleted)
        {
            if (isCompleted)
            {
                Complete();
            }
            else
            {
                Abort();
            }
            
            Callback_?.Invoke(isCompleted);
            Completed?.Invoke(isCompleted);
        }

        public void AddSubTask(ITask task)
        {
            SubTaskList_.Add(task);
        }
    }
}