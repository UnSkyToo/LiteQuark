namespace LiteQuark.Runtime
{
    public interface IAction
    {
        ulong ID { get; }
        bool IsDone { get; }
        void Cancel();
    }
}
