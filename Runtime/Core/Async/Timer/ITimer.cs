namespace LiteQuark.Runtime
{
    public interface ITimer : ITick
    {
        ulong ID { get; }
        bool IsDone { get; }
        
        public void Pause();
        public void Resume();
        
        public void Cancel();
    }
}