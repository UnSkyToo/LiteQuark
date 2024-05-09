namespace LiteQuark.Runtime
{
    public interface IAction : ITick, IDispose
    {
        ulong ID { get; }
        bool IsEnd { get; }

        void MarkSafety();
        void Stop();
        void Execute();
    }
}