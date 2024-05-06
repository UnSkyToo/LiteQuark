namespace LiteQuark.Runtime
{
    public interface IFsmState : ITick, IDispose
    {
        int ID { get; }
        IFsm Fsm { get; set; }

        void Enter(params object[] args);
        void Leave();
        
        bool GotoCheck(int targetID);
    }
}