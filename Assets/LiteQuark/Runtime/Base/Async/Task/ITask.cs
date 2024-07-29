namespace LiteQuark.Runtime
{
    public interface ITask : ITick, IDispose
    {
        public TaskState State { get; }
        
        public void Execute();
    }
}