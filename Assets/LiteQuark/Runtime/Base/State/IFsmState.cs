namespace LiteQuark.Runtime
{
    public interface IFsmState : ITick
    {
        int ID { get; }

        void Enter(params object[] args);
        void Leave();
        
        bool GotoCheck(int targetID);
    }
}