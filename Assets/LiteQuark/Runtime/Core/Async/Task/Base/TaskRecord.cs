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
        public ITask Task { get; }
        public TaskGroup Group { get; }
        public TaskSchedulePhase Phase { get; set; }

        public TaskRecord(ITask task, TaskGroup group)
        {
            Task = task;
            Group = group;
            Phase = TaskSchedulePhase.Pending;
        }
    }
}
