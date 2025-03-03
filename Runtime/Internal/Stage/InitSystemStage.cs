namespace LiteQuark.Runtime
{
    internal sealed class InitSystemStage : IStage
    {
        private StageCode CurrentCode_ = StageCode.Waiting;
        
        public InitSystemStage()
        {
        }
        
        public void Enter()
        {
            CurrentCode_ = StageCode.Waiting;
            SystemCenter.Instance.InitializeSystem(OnCallback);
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