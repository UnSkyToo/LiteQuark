namespace LiteQuark.Runtime
{
    public class CallbackAction : BaseAction
    {
        public override string DebugName => "<Callback>";

        private readonly System.Action Callback_ = null;
        
        public CallbackAction(System.Action callback)
        {
            Callback_ = callback;
        }

        public override void Execute()
        {
            IsEnd = true;
            Callback_?.Invoke();
        }
    }
    
    public class CallbackAction<T> : BaseAction where T : struct
    {
        public override string DebugName => "<Callback>()";

        private readonly System.Action<T> Callback_ = null;
        private readonly T Param_ = default;
        
        public CallbackAction(System.Action<T> callback, T param)
        {
            Callback_ = callback;
            Param_ = param;
        }

        public override void Execute()
        {
            IsEnd = true;
            Callback_?.Invoke(Param_);
        }
    }
}