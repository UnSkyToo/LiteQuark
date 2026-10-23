namespace LiteQuark.Runtime
{
    internal enum TaskSchedulePhase
    {
        Pending,
        Running,
        Retired,
    }

    internal sealed class TaskRecord
    {
        public BaseTask Task { get; }
        public TaskGroup Group { get; }
        public TaskPriority Priority { get; }
        public TaskSchedulePhase Phase { get; set; }

        public TaskRecord(BaseTask task, TaskGroup group, TaskPriority priority)
        {
            Task = task;
            Group = group;
            Priority = priority;
            Phase = TaskSchedulePhase.Pending;
        }
    }
}
