namespace LiteQuark.Runtime
{
    public interface IAction : ITick, IDispose
    {
        ulong ID { get; }
        bool IsEnd { get; }
        void Execute();
        void Stop();
    }
}