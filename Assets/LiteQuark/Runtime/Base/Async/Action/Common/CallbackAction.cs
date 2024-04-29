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
}