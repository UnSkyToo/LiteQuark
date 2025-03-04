namespace LiteQuark.Runtime
{
    internal sealed class InitSystemStage : IStage
    {
        private StageCode CurrentCode_ = StageCode.Waiting;
        
        public InitSystemStage()
        {
        }
        
        public async void Enter()
        {
            CurrentCode_ = StageCode.Waiting;
            var result = await SystemCenter.Instance.InitializeSystem();
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