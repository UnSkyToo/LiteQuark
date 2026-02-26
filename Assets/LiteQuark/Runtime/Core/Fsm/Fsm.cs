using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public class Fsm : IFsm
    {
        protected readonly Dictionary<int, IFsmState> StateMap = new Dictionary<int, IFsmState>();
        protected IFsmState CurrentState = null;

        public Fsm()
        {
        }

        public void Dispose()
        {
            CurrentState?.Leave();

            foreach (var chunk in StateMap)
            {
                chunk.Value.Dispose();
            }
            StateMap.Clear();
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime);
        }

        public void AddState(int id, IFsmState state)
        {
            state.Fsm = this;
            StateMap.TryAdd(id, state);
        }

        public IFsmState GetState(int id)
        {
            return StateMap.GetValueOrDefault(id);
        }

        public void ChangeToNullState()
        {
            if (CurrentState != null)
            {
                CurrentState.Leave();
                CurrentState = null;
            }
        }

        public bool ChangeToState(int id, params object[] args)
        {
            if (CurrentState != null)
            {
                if (!CurrentState.CanChangeTo(id))
                {
                    return false;
                }
                
                CurrentState.Leave();
            }
            
            StateMap.TryGetValue(id, out CurrentState);
            CurrentState?.Enter(args);
            return true;
        }

        public bool IsState(int id)
        {
            if (CurrentState == null)
            {
                return false;
            }

            return CurrentState.ID == id;
        }

        public IFsmState GetCurrentState()
        {
            return CurrentState;
        }
    }
}