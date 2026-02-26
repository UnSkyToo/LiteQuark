namespace LiteQuark.Runtime
{
    public abstract class FsmBaseState : IFsmState
    {
        public abstract int ID { get; }
        public IFsm Fsm { get; set; }
        
        protected FsmBaseState()
        {
        }

        public virtual void Dispose()
        {
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

        public virtual bool CanChangeTo(int targetID)
        {
            return true;
        }
    }
}