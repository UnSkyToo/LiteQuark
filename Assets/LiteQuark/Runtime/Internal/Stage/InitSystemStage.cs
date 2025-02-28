namespace LiteQuark.Runtime
{
    internal sealed class InitSystemStage : BaseStage
    {
        public InitSystemStage()
        {
        }
        
        public override void Enter()
        {
            SystemCenter.Instance.InitializeSystem();
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