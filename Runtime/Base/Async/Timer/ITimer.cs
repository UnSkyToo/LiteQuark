namespace LiteQuark.Runtime
{
    public interface ITimer : ITick
    {
        ulong ID { get; }
        bool IsEnd { get; }
        
        public void Pause();
        public void Resume();
        
        public void Cancel();
    }
}