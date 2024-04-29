namespace LiteQuark.Runtime
{
    public interface IAction : ITick, IDispose
    {
        bool IsEnd { get; }
        void Execute();
    }
}