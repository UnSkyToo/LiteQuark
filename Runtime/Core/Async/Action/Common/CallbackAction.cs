namespace LiteQuark.Runtime
{
    public class CallbackAction : BaseAction
    {
        public override string DebugName => "<Callback>";

        private readonly System.Action _callback = null;
        
        public CallbackAction(System.Action callback)
        {
            _callback = callback;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke();
        }
    }
    
    // public class CallbackFuncAction<T> : BaseAction
    // {
    //     public override string DebugName => "<Callback>()";
    //
    //     private readonly System.Func<T> Callback_ = null;
    //     
    //     public CallbackFuncAction(System.Func<T> callback)
    //     {
    //         Callback_ = callback;
    //     }
    //
    //     public override void Execute()
    //     {
    //         IsEnd = true;
    //         Callback_?.Invoke();
    //     }
    // }
    
    public class CallbackAction<T> : BaseAction
    {
        public override string DebugName => "<Callback1>()";

        private readonly System.Action<T> _callback = null;
        private readonly T _param = default;
        
        public CallbackAction(System.Action<T> callback, T param)
        {
            _callback = callback;
            _param = param;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param);
        }
    }
    
    public class CallbackFuncAction<T, TR> : BaseAction
    {
        public override string DebugName => "<Callback1>()";

        private readonly System.Func<T, TR> _callback = null;
        private readonly T _param = default;
        
        public CallbackFuncAction(System.Func<T, TR> callback, T param)
        {
            _callback = callback;
            _param = param;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param);
        }
    }
    
    public class CallbackAction<T1, T2> : BaseAction
    {
        public override string DebugName => "<Callback2>()";

        private readonly System.Action<T1, T2> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        
        public CallbackAction(System.Action<T1, T2> callback, T1 param1, T2 param2)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param1, _param2);
        }
    }
    
    public class CallbackFuncAction<T1, T2, TR> : BaseAction
    {
        public override string DebugName => "<Callback2>()";

        private readonly System.Func<T1, T2, TR> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        
        public CallbackFuncAction(System.Func<T1, T2, TR> callback, T1 param1, T2 param2)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param1, _param2);
        }
    }
    
    public class CallbackAction<T1, T2, T3> : BaseAction
    {
        public override string DebugName => "<Callback3>()";

        private readonly System.Action<T1, T2, T3> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        private readonly T3 _param3 = default;
        
        public CallbackAction(System.Action<T1, T2, T3> callback, T1 param1, T2 param2, T3 param3)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param1, _param2, _param3);
        }
    }
    
    public class CallbackFuncAction<T1, T2, T3, TR> : BaseAction
    {
        public override string DebugName => "<Callback3>()";

        private readonly System.Func<T1, T2, T3, TR> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        private readonly T3 _param3 = default;
        
        public CallbackFuncAction(System.Func<T1, T2, T3, TR> callback, T1 param1, T2 param2, T3 param3)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param1, _param2, _param3);
        }
    }
    
    public class CallbackAction<T1, T2, T3, T4> : BaseAction
    {
        public override string DebugName => "<Callback4>()";

        private readonly System.Action<T1, T2, T3, T4> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        private readonly T3 _param3 = default;
        private readonly T4 _param4 = default;
        
        public CallbackAction(System.Action<T1, T2, T3, T4> callback, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
            _param4 = param4;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param1, _param2, _param3, _param4);
        }
    }
    
    public class CallbackFuncAction<T1, T2, T3, T4, TR> : BaseAction
    {
        public override string DebugName => "<Callback4>()";

        private readonly System.Func<T1, T2, T3, T4, TR> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        private readonly T3 _param3 = default;
        private readonly T4 _param4 = default;
        
        public CallbackFuncAction(System.Func<T1, T2, T3, T4, TR> callback, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
            _param4 = param4;
        }

        public override void Execute()
        {
            IsEnd = true;
            _callback?.Invoke(_param1, _param2, _param3, _param4);
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder Callback(this ActionBuilder builder, System.Action callback)
        {
            builder.Add(new CallbackAction(callback));
            return builder;
        }

        // public static ActionBuilder Callback<TR>(this ActionBuilder builder, System.Func<TR> callback)
        // {
        //     builder.Add(new CallbackFuncAction<TR>(callback));
        //     return builder;
        // }

        public static ActionBuilder Callback<T>(this ActionBuilder builder, System.Action<T> callback, T param)
        {
            builder.Add(new CallbackAction<T>(callback, param));
            return builder;
        }

        public static ActionBuilder Callback<T, TR>(this ActionBuilder builder, System.Func<T, TR> callback, T param)
        {
            builder.Add(new CallbackFuncAction<T, TR>(callback, param));
            return builder;
        }

        public static ActionBuilder Callback<T1, T2>(this ActionBuilder builder, System.Action<T1, T2> callback, T1 param1, T2 param2)
        {
            builder.Add(new CallbackAction<T1, T2>(callback, param1, param2));
            return builder;
        }

        public static ActionBuilder Callback<T1, T2, TR>(this ActionBuilder builder, System.Func<T1, T2, TR> callback, T1 param1, T2 param2)
        {
            builder.Add(new CallbackFuncAction<T1, T2, TR>(callback, param1, param2));
            return builder;
        }

        public static ActionBuilder Callback<T1, T2, T3>(this ActionBuilder builder, System.Action<T1, T2, T3> callback, T1 param1, T2 param2, T3 param3)
        {
            builder.Add(new CallbackAction<T1, T2, T3>(callback, param1, param2, param3));
            return builder;
        }

        public static ActionBuilder Callback<T1, T2, T3, TR>(this ActionBuilder builder, System.Func<T1, T2, T3, TR> callback, T1 param1, T2 param2, T3 param3)
        {
            builder.Add(new CallbackFuncAction<T1, T2, T3, TR>(callback, param1, param2, param3));
            return builder;
        }
        
        public static ActionBuilder Callback<T1, T2, T3, T4>(this ActionBuilder builder, System.Action<T1, T2, T3, T4> callback, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            builder.Add(new CallbackAction<T1, T2, T3, T4>(callback, param1, param2, param3, param4));
            return builder;
        }

        public static ActionBuilder Callback<T1, T2, T3, T4, TR>(this ActionBuilder builder, System.Func<T1, T2, T3, T4, TR> callback, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            builder.Add(new CallbackFuncAction<T1, T2, T3, T4, TR>(callback, param1, param2, param3, param4));
            return builder;
        }
    }
}