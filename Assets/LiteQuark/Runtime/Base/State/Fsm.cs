using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class Fsm : IFsm
    {
        protected readonly Dictionary<int, IFsmState> StateMap_ = new Dictionary<int, IFsmState>();
        protected IFsmState CurrentState_ = default;

        public Fsm()
        {
        }

        public void Dispose()
        {
            CurrentState_?.Leave();

            foreach (var chunk in StateMap_)
            {
                chunk.Value.Dispose();
            }
            StateMap_.Clear();
        }

        public void Tick(float deltaTime)
        {
            CurrentState_?.Tick(deltaTime);
        }

        public void AddState(int id, IFsmState state)
        {
            state.Fsm = this;
            StateMap_.TryAdd(id, state);
        }

        public IFsmState GetState(int id)
        {
            return StateMap_.GetValueOrDefault(id);
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