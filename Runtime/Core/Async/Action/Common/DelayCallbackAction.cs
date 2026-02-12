namespace LiteQuark.Runtime
{
    public abstract class DelayCallbackBaseAction : BaseAction
    {
        public override string DebugName => $"<DelayCallback>({_currentTime}/{_waitTime})";
        
        private readonly float _waitTime = 0f;
        private float _currentTime = 0f;

        protected DelayCallbackBaseAction(float waitTime)
        {
            _waitTime = waitTime;
        }

        public override void Execute()
        {
            _currentTime = _waitTime;
            IsEnd = _currentTime <= 0f;
        }

        public override void Tick(float deltaTime)
        {
            _currentTime -= deltaTime;
            if (_currentTime <= 0f)
            {
                IsEnd = true;
                OnCallback();
            }
        }

        protected abstract void OnCallback();
    }

    public class DelayCallbackAction : DelayCallbackBaseAction
    {
        private readonly System.Action _callback = null;

        public DelayCallbackAction(float waitTime, System.Action callback)
            : base(waitTime)
        {
            _callback = callback;
        }

        protected override void OnCallback()
        {
            _callback?.Invoke();
        }
    }

    public class DelayCallbackAction<T> : DelayCallbackBaseAction
    {
        private readonly System.Action<T> _callback = null;
        private readonly T _param = default;

        public DelayCallbackAction(float waitTime, System.Action<T> callback, T param)
            : base(waitTime)
        {
            _callback = callback;
            _param = param;
        }

        protected override void OnCallback()
        {
            _callback?.Invoke(_param);
        }
    }

    public class DelayCallbackAction<T1, T2> : DelayCallbackBaseAction
    {
        private readonly System.Action<T1, T2> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;

        public DelayCallbackAction(float waitTime, System.Action<T1, T2> callback, T1 param1, T2 param2)
            : base(waitTime)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
        }

        protected override void OnCallback()
        {
            _callback?.Invoke(_param1, _param2);
        }
    }

    public class DelayCallbackAction<T1, T2, T3> : DelayCallbackBaseAction
    {
        private readonly System.Action<T1, T2, T3> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        private readonly T3 _param3 = default;

        public DelayCallbackAction(float waitTime, System.Action<T1, T2, T3> callback, T1 param1, T2 param2, T3 param3)
            : base(waitTime)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
        }

        protected override void OnCallback()
        {
            _callback?.Invoke(_param1, _param2, _param3);
        }
    }
    
    public class DelayCallbackAction<T1, T2, T3, T4> : DelayCallbackBaseAction
    {
        private readonly System.Action<T1, T2, T3, T4> _callback = null;
        private readonly T1 _param1 = default;
        private readonly T2 _param2 = default;
        private readonly T3 _param3 = default;
        private readonly T4 _param4 = default;
        
        public DelayCallbackAction(float waitTime, System.Action<T1, T2, T3, T4> callback, T1 param1, T2 param2, T3 param3, T4 param4)
            : base(waitTime)
        {
            _callback = callback;
            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
            _param4 = param4;
        }

        protected override void OnCallback()
        {
            _callback?.Invoke(_param1, _param2, _param3, _param4);
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder DelayCallback(this ActionBuilder builder, float waitTime, System.Action callback)
        {
            builder.Add(new DelayCallbackAction(waitTime, callback));
            return builder;
        }

        public static ActionBuilder DelayCallback<T>(this ActionBuilder builder, float waitTime, System.Action<T> callback, T param)
        {
            builder.Add(new DelayCallbackAction<T>(waitTime, callback, param));
            return builder;
        }

        public static ActionBuilder DelayCallback<T1, T2>(this ActionBuilder builder, float waitTime, System.Action<T1, T2> callback, T1 param1, T2 param2)
        {
            builder.Add(new DelayCallbackAction<T1, T2>(waitTime, callback, param1, param2));
            return builder;
        }

        public static ActionBuilder DelayCallback<T1, T2, T3>(this ActionBuilder builder, float waitTime, System.Action<T1, T2, T3> callback, T1 param1, T2 param2, T3 param3)
        {
            builder.Add(new DelayCallbackAction<T1, T2, T3>(waitTime, callback, param1, param2, param3));
            return builder;
        }
        
        public static ActionBuilder DelayCallback<T1, T2, T3, T4>(this ActionBuilder builder, float waitTime, System.Action<T1, T2, T3, T4> callback, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            builder.Add(new DelayCallbackAction<T1, T2, T3, T4>(waitTime, callback, param1, param2, param3, param4));
            return builder;
        }
    }
}