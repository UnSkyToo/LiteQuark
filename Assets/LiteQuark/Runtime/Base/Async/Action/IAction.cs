namespace LiteQuark.Runtime
{
    public interface IAction : ITick, IDispose
    {
        ulong ID { get; }
        bool IsEnd { get; }
        System.Action FinalCallback { get; }

        void MarkSafety();
        void Stop();
        void Execute();
        
        IAction SetFinalCallback(System.Action callback);
    }
}