namespace LiteQuark.Runtime
{
    internal sealed class InitSystemStage : IStage
    {
        private StageCode _currentCode = StageCode.Waiting;
        
        public InitSystemStage()
        {
        }
        
        public async void Enter()
        {
            _currentCode = StageCode.Waiting;
            var result = await SystemCenter.Instance.InitializeSystem();
            _currentCode = result ? StageCode.Completed : StageCode.Error;
        }

        public void Leave()
        {
        }

        public StageCode Tick(float deltaTime)
        {
            return _currentCode;
        }
    }
}