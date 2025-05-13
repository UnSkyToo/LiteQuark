using System;

namespace LiteQuark.Runtime
{
    public sealed class SequenceTask : BaseTask
    {
        private readonly ITask[] _subTasks = null;
        private readonly Action<bool> _callback = null;
        private ITask _currentSubTask = null;
        private int _index = 0;
        
        public SequenceTask(ITask[] subTasks, Action<bool> callback)
            : base()
        {
            _subTasks = subTasks ?? Array.Empty<ITask>();
            _callback = callback;
            _index = 0;
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
            _index = 0;
            NextTask();
        }

        protected override void OnTick(float deltaTime)
        {
            if (_currentSubTask == null)
            {
                Complete(true);
                return;
            }
            
            if (_currentSubTask.State == TaskState.Completed)
            {
                NextTask();
            }
            else if (_currentSubTask.State == TaskState.Aborted)
            {
                MarkResult(false);
            }
        }

        private void NextTask()
        {
            Progress = (float)_index / (float)_subTasks.Length;

            if (_index >= _subTasks.Length)
            {
                _currentSubTask = null;
                MarkResult(true);
                return;
            }
            
            _currentSubTask = _subTasks[_index++];
            _currentSubTask?.Execute();
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
            
            _callback?.Invoke(isCompleted);
        }
    }
}