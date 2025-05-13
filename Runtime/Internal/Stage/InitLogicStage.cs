namespace LiteQuark.Runtime
{
    internal sealed class InitLogicStage : IStage
    {
        private StageCode _currentCode = StageCode.Waiting;
        
        public InitLogicStage()
        {
        }
        
        public async void Enter()
        {
            _currentCode = StageCode.Waiting;
            var result = await LogicCenter.Instance.InitializeLogic();
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