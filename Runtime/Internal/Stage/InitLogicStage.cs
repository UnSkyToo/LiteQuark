namespace LiteQuark.Runtime
{
    internal sealed class InitLogicStage : IStage
    {
        private StageCode CurrentCode_ = StageCode.Waiting;
        
        public InitLogicStage()
        {
        }
        
        public void Enter()
        {
            CurrentCode_ = StageCode.Waiting;
            LogicCenter.Instance.InitializeLogic(OnCallback);
        }

        public void Leave()
        {
        }

        public StageCode Tick(float deltaTime)
        {
            return CurrentCode_;
        }

        private void OnCallback(bool result)
        {
            CurrentCode_ = result ? StageCode.Completed : StageCode.Error;
        }
    }
}