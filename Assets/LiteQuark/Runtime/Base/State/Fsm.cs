using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class Fsm : ITick, IDispose
    {
        protected readonly Dictionary<int, IFsmState> StateMap_ = new Dictionary<int, IFsmState>();
        protected IFsmState CurrentState_ = default;

        public Fsm()
        {
        }

        public void Dispose()
        {
            CurrentState_?.Leave();
        }

        public void Tick(float deltaTime)
        {
            CurrentState_?.Tick(deltaTime);
        }

        public void AddState(int id, IFsmState state)
        {
            StateMap_.TryAdd(id, state);
        }

        public bool ChangeToState(int id, params object[] args)
        {
            if (CurrentState_ != default)
            {
                if (!CurrentState_.GotoCheck(id))
                {
                    return false;
                }
                
                CurrentState_.Leave();
            }
            
            StateMap_.TryGetValue(id, out CurrentState_);
            CurrentState_?.Enter(args);
            return true;
        }

        public bool IsState(int id)
        {
            if (CurrentState_ == default)
            {
                return false;
            }

            return CurrentState_.ID == id;
        }

        public IFsmState GetCurrentState()
        {
            return CurrentState_;
        }
    }
}