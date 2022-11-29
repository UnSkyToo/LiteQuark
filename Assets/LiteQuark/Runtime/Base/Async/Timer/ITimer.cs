namespace LiteQuark.Runtime
{
    public interface ITimer : ITick
    {
        bool IsEnd { get; }
        
        public void Pause();
        public void Resume();
        
        public void Cancel();
    }
}