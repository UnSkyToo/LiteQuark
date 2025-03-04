namespace LiteQuark.Runtime
{
    internal sealed class InitLogicStage : IStage
    {
        private StageCode CurrentCode_ = StageCode.Waiting;
        
        public InitLogicStage()
        {
        }
        
        public async void Enter()
        {
            CurrentCode_ = StageCode.Waiting;
            var result = await LogicCenter.Instance.InitializeLogic();
            CurrentCode_ = result ? StageCode.Completed : StageCode.Error;
        }

        public void Leave()
        {
        }

        public StageCode Tick(float deltaTime)
        {
            return CurrentCode_;
        }
    }
}