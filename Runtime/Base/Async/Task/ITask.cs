namespace LiteQuark.Runtime
{
    public interface ITask : IDispose
    {
        public bool IsEnd { get; }
        public bool IsExecute { get; }
        
        public void Execute();
    }
}