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
            IsDone = true;
            LiteUtils.SafeInvoke(_callback);
        }
    }
    
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
            IsDone = true;
            LiteUtils.SafeInvoke(_callback, _param);
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
            IsDone = true;
            LiteUtils.SafeInvoke(_callback, _param1, _param2);
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
            IsDone = true;
            LiteUtils.SafeInvoke(_callback, _param1, _param2, _param3);
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder Callback(this ActionBuilder builder, System.Action callback)
        {
            builder.Add(new CallbackAction(callback));
            return builder;
        }

        public static ActionBuilder Callback<T>(this ActionBuilder builder, System.Action<T> callback, T param)
        {
            builder.Add(new CallbackAction<T>(callback, param));
            return builder;
        }

        public static ActionBuilder Callback<T1, T2>(this ActionBuilder builder, System.Action<T1, T2> callback, T1 param1, T2 param2)
        {
            builder.Add(new CallbackAction<T1, T2>(callback, param1, param2));
            return builder;
        }

        public static ActionBuilder Callback<T1, T2, T3>(this ActionBuilder builder, System.Action<T1, T2, T3> callback, T1 param1, T2 param2, T3 param3)
        {
            builder.Add(new CallbackAction<T1, T2, T3>(callback, param1, param2, param3));
            return builder;
        }
    }
}