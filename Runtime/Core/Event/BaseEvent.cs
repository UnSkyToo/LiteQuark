namespace LiteQuark.Runtime
{
    public abstract class BaseEvent
    {
        public string EventName { get; }
        
        protected BaseEvent()
        {
            EventName = GetType().Name;
        }
    }
}