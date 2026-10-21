using System;

namespace LiteQuark.Runtime
{
    internal sealed class TaskGroup
    {
        public int Id { get; }
        public int? ConcurrencyLimit { get; }

        public int PendingTaskCount => _pendingTaskQueue.Count;
        public int RunningTaskCount { get; private set; }

        private readonly PriorityQueue<TaskRecord> _pendingTaskQueue;

        public TaskGroup(int id, int? concurrencyLimit)
        {
            if (concurrencyLimit is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLimit));
            }

            Id = id;
            ConcurrencyLimit = concurrencyLimit;
            _pendingTaskQueue = new PriorityQueue<TaskRecord>(static (x, y) => ((int)x.Task.Priority).CompareTo((int)y.Task.Priority));
        }

        public void Enqueue(TaskRecord record)
        {
            _pendingTaskQueue.Enqueue(record);
        }

        public bool TryAdmit(out TaskRecord record)
        {
            record = null;

            if (RunningTaskCount >= ConcurrencyLimit || _pendingTaskQueue.Count == 0)
            {
                return false;
            }

            record = _pendingTaskQueue.Dequeue();
            record.Phase = TaskSchedulePhase.Running;
            RunningTaskCount++;

            return true;
        }

        public void Detach(TaskRecord record)
        {
            if (record.Phase == TaskSchedulePhase.Retired)
            {
                return;
            }

            if (record.Phase == TaskSchedulePhase.Pending)
            {
                _pendingTaskQueue.Remove(record);
            }
            else
            {
                RunningTaskCount--;
            }

            record.Phase = TaskSchedulePhase.Retired;
        }
    }
}
