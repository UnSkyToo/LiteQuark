namespace LiteQuark.Runtime
{
    public interface IFsm : ITick, IDispose
    {
        void AddState(int id, IFsmState state);
        bool ChangeToState(int id, params object[] args);
        bool IsState(int id);
        IFsmState GetCurrentState();
    }
}