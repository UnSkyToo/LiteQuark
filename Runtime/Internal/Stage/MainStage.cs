namespace LiteQuark.Runtime
{
    internal sealed class MainStage : BaseStage
    {
        public MainStage()
        {
        }
        
        public override void Enter()
        {
        }

        public override void Leave()
        {
            LogicCenter.Instance.Dispose();
            SystemCenter.Instance.Dispose();
        }

        public override StageCode Tick(float deltaTime)
        {
            SystemCenter.Instance.Tick(deltaTime);
            LogicCenter.Instance.Tick(deltaTime);
            
            return StageCode.Running;
        }
    }
}