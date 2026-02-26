namespace LiteQuark.Runtime
{
    public interface IAction : ITick, IDispose
    {
        ulong ID { get; }
        bool IsDone { get; }
        System.Action<IAction> FinalCallback { get; }

        void MarkAsSafe();
        void Stop();
        void Execute();
        
        IAction SetFinalCallback(System.Action<IAction> callback);
    }
}