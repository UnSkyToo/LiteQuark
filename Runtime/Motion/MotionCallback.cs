using System;

namespace LiteQuark.Runtime
{
    public class MotionCallback : MotionBase
    {
        private readonly Action Callback_;

        public MotionCallback(Action callback)
            : base()
        {
            Callback_ = callback;
        }

        public override void Enter()
        {
            Callback_?.Invoke();
        }
    }

    public class MotionCallback<T> : MotionBase
    {
        private readonly Action<T> Callback_;
        private readonly T Param_;

        public MotionCallback(Action<T> callback, T param)
            : base()
        {
            Callback_ = callback;
            Param_ = param;
        }

        public override void Enter()
        {
            Callback_?.Invoke(Param_);
        }
    }

    public class MotionCallback<T1, T2> : MotionBase
    {
        private readonly Action<T1, T2> Callback_;
        private readonly T1 Param1_;
        private readonly T2 Param2_;

        public MotionCallback(Action<T1, T2> callback, T1 param1, T2 param2)
            : base()
        {
            Callback_ = callback;
            Param1_ = param1;
            Param2_ = param2;
        }

        public override void Enter()
        {
            Callback_?.Invoke(Param1_, Param2_);
        }
    }
}