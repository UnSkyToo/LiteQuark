using System;

namespace LiteQuark.Runtime
{
    public abstract class FsmEnumBaseState<T> : FsmBaseState where T : struct, Enum, IConvertible
    {
        public override int ID => Convert.ToInt32(State);
        public abstract T State { get; }
        
        protected FsmEnumBaseState()
            : base()
        {
        }

        public override bool CanChangeTo(int targetID)
        {
            var state = (T)Enum.ToObject(typeof(T), targetID);
            return CanChangeTo(state);
        }

        public virtual bool CanChangeTo(T state)
        {
            return true;
        }
    }
}