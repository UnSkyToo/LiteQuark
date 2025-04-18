using System;

namespace LiteQuark.Runtime
{
    public sealed class SequenceTask : BaseTask
    {
        private readonly ITask[] SubTasks_ = null;
        private readonly Action<bool> Callback_ = null;
        private ITask CurrentSubTask_ = null;
        private int Index_ = 0;
        
        public SequenceTask(ITask[] subTasks, Action<bool> callback)
            : base()
        {
            SubTasks_ = subTasks ?? Array.Empty<ITask>();
            Callback_ = callback;
            Index_ = 0;
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
            Index_ = 0;
            NextTask();
        }

        protected override void OnTick(float deltaTime)
        {
            if (CurrentSubTask_ == null)
            {
                Complete(true);
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
            Progress = (float)Index_ / (float)SubTasks_.Length;

            if (Index_ >= SubTasks_.Length)
            {
                CurrentSubTask_ = null;
                MarkResult(true);
                return;
            }
            
            CurrentSubTask_ = SubTasks_[Index_++];
            CurrentSubTask_?.Execute();
        }

        private void MarkResult(bool isCompleted)
        {
            if (isCompleted)
            {
                Complete(true);
            }
            else
            {
                Abort();
            }
            
            Callback_?.Invoke(isCompleted);
        }
    }
}