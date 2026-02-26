namespace LiteQuark.Runtime
{
    public interface ITimer : ITick
    {
        ulong ID { get; }
        bool IsDone { get; }
        bool IsUnscaled { get; }
        
        void Pause();
        void Resume();
        void Cancel();
    }
}