using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public interface IPipelineSubTask : IDispose
    {
        bool IsEnd { get; }
        
        void Execute();
    }
    
    public sealed class PipelineTask : BaseTask
    {
        public event Action<int, int> Progress;
        public event Action Completed;
        
        private readonly List<IPipelineSubTask> SubTaskList_ = null;
        private IPipelineSubTask CurrentSubTask_ = null;
        private int Index_ = 0;
        
        public PipelineTask(IPipelineSubTask[] subTasks)
            : base()
        {
            SubTaskList_ = new List<IPipelineSubTask>(subTasks);
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
                State = TaskState.Completed;
                return;
            }
            
            if (CurrentSubTask_.IsEnd)
            {
                NextTask();
            }
        }

        private void NextTask()
        {
            Progress?.Invoke(Index_, SubTaskList_.Count);

            if (Index_ >= SubTaskList_.Count)
            {
                CurrentSubTask_ = null;
                State = TaskState.Completed;
                Completed?.Invoke();
                return;
            }
            
            CurrentSubTask_ = SubTaskList_[Index_++];
            CurrentSubTask_?.Execute();
        }

        public void AddSubTask(IPipelineSubTask task)
        {
            SubTaskList_.Add(task);
        }
    }
}