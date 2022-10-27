namespace LiteQuark.Runtime
{
    public abstract class EventBase
    {
        public string EventName { get; }
        
        protected EventBase()
        {
            EventName = GetType().Name;
        }
    }
}