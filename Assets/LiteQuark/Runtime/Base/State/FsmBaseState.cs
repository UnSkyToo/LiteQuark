namespace LiteQuark.Runtime
{
    public abstract class FsmBaseState : IFsmState
    {
        public abstract int ID { get; }
        protected readonly Fsm Fsm_;
        
        protected FsmBaseState(Fsm fsm)
        {
            Fsm_ = fsm;
        }
        
        public virtual void Enter(params object[] args)
        {
        }

        public virtual void Leave()
        {
        }
        
        public virtual void Tick(float deltaTime)
        {
        }

        public virtual bool GotoCheck(int targetID)
        {
            return true;
        }
    }
}