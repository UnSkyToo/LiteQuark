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
    
    public class CallbackFuncAction<T> : BaseAction
    {
        public override string DebugName => "<Callback>()";

        private readonly System.Func<T> Callback_ = null;
        
        public CallbackFuncAction(System.Func<T> callback)
        {
            Callback_ = callback;
        }

        public override void Execute()
        {
            IsEnd = true;
            Callback_?.Invoke();
        }
    }
    
    public class CallbackAction<T> : BaseAction
    {
        public override string DebugName => "<Callback1>()";

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
    
    public class CallbackFuncAction<T, TR> : BaseAction
    {
        public override string DebugName => "<Callback1>()";

        private readonly System.Func<T, TR> Callback_ = null;
        private readonly T Param_ = default;
        
        public CallbackFuncAction(System.Func<T, TR> callback, T param)
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
    
    public class CallbackAction<T1, T2> : BaseAction
    {
        public override string DebugName => "<Callback2>()";

        private readonly System.Action<T1, T2> Callback_ = null;
        private readonly T1 Param1_ = default;
        private readonly T2 Param2_ = default;
        
        public CallbackAction(System.Action<T1, T2> callback, T1 param1, T2 param2)
        {
            Callback_ = callback;
            Param1_ = param1;
            Param2_ = param2;
        }

        public override void Execute()
        {
            IsEnd = true;
            Callback_?.Invoke(Param1_, Param2_);
        }
    }
    
    public class CallbackFuncAction<T1, T2, TR> : BaseAction
    {
        public override string DebugName => "<Callback2>()";

        private readonly System.Func<T1, T2, TR> Callback_ = null;
        private readonly T1 Param1_ = default;
        private readonly T2 Param2_ = default;
        
        public CallbackFuncAction(System.Func<T1, T2, TR> callback, T1 param1, T2 param2)
        {
            Callback_ = callback;
            Param1_ = param1;
            Param2_ = param2;
        }

        public override void Execute()
        {
            IsEnd = true;
            Callback_?.Invoke(Param1_, Param2_);
        }
    }
    
    public class CallbackAction<T1, T2, T3> : BaseAction
    {
        public override string DebugName => "<Callback3>()";

        private readonly System.Action<T1, T2, T3> Callback_ = null;
        private readonly T1 Param1_ = default;
        private readonly T2 Param2_ = default;
        private readonly T3 Param3_ = default;
        
        public CallbackAction(System.Action<T1, T2, T3> callback, T1 param1, T2 param2, T3 param3)
        {
            Callback_ = callback;
            Param1_ = param1;
            Param2_ = param2;
            Param3_ = param3;
        }

        public override void Execute()
        {
            IsEnd = true;
            Callback_?.Invoke(Param1_, Param2_, Param3_);
        }
    }
    
    public class CallbackFuncAction<T1, T2, T3, TR> : BaseAction
    {
        public override string DebugName => "<Callback3>()";

        private readonly System.Func<T1, T2, T3, TR> Callback_ = null;
        private readonly T1 Param1_ = default;
        private readonly T2 Param2_ = default;
        private readonly T3 Param3_ = default;
        
        public CallbackFuncAction(System.Func<T1, T2, T3, TR> callback, T1 param1, T2 param2, T3 param3)
        {
            Callback_ = callback;
            Param1_ = param1;
            Param2_ = param2;
            Param3_ = param3;
        }

        public override void Execute()
        {
            IsEnd = true;
            Callback_?.Invoke(Param1_, Param2_, Param3_);
        }
    }
}