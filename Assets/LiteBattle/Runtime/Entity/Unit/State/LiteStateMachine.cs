using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public sealed class LiteStateMachine
    {
        public LiteUnit Unit { get; }
        public string StateGroupID { get; }

        private LiteState CurrentState_ = null;
        private string NextStateID_ = string.Empty;

        public LiteStateMachine(LiteUnit unit, string stateGroupID)
        {
            Unit = unit;
            StateGroupID = stateGroupID;
        }

        public LiteState GetCurrentState()
        {
            return CurrentState_;
        }

        public void SetNextState(string stateID)
        {
            NextStateID_ = stateID;
        }

        public void Tick(float deltaTime)
        {
            if (!string.IsNullOrWhiteSpace(NextStateID_))
            {
                ChangeToState(StateGroupID, NextStateID_);
                NextStateID_ = string.Empty;
            }
            
            CurrentState_?.Tick(deltaTime);

            if (CurrentState_?.IsEnd() == true && string.IsNullOrWhiteSpace(NextStateID_))
            {
                LLog.Error($"{CurrentState_?.Name} is end, but there is no state to be transferred");
                ChangeToState(null, null);
            }
        }

        private void ChangeToState(string stateGroupID, string stateID)
        {
            CurrentState_?.Leave();
            
            var stateConfig = LiteNexusDataManager.Instance.GetStateConfig(stateGroupID, stateID);
            if (stateConfig != null)
            {
                CurrentState_ = new LiteState(stateConfig);
                CurrentState_?.Enter(this);
            }
        }
    }
}