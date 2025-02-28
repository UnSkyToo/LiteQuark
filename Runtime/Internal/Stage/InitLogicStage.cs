namespace LiteQuark.Runtime
{
    internal sealed class InitLogicStage : BaseStage
    {
        public InitLogicStage()
        {
        }
        
        public override void Enter()
        {
            LogicCenter.Instance.InitializeLogic();
        }

        public override void Leave()
        {
        }

        public override StageCode Tick(float deltaTime)
        {
            return StageCode.Completed;
        }
    }
}