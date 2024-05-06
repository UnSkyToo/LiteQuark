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

        public override bool GotoCheck(int targetID)
        {
            var state = (T)Enum.ToObject(typeof(T), targetID);
            return GotoCheck(state);
        }

        public virtual bool GotoCheck(T state)
        {
            return true;
        }
    }
}