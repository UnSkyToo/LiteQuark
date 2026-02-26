namespace LiteQuark.Runtime
{
    public interface IFsmState : ISubstance
    {
        int ID { get; }
        IFsm Fsm { get; set; }
        
        void Enter(params object[] args);
        void Leave();
        
        bool CanChangeTo(int targetID);
    }
}