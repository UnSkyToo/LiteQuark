namespace LiteQuark.Runtime
{
    public enum FrameworkErrorCode
    {
        Unknown,
        Startup,
        NetError,
    }
    
    public sealed class FrameworkErrorEvent : IEventData
    {
        public FrameworkErrorCode ErrorCode { get; }
        public string Message { get; }
        
        public FrameworkErrorEvent(FrameworkErrorCode errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }
    }
    
    public sealed class EnterForegroundEvent : IEventData
    {
    }

    public sealed class EnterBackgroundEvent : IEventData
    {
    }
}