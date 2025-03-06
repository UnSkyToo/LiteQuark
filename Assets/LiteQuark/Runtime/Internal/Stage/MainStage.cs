namespace LiteQuark.Runtime
{
    internal sealed class MainStage : IStage
    {
        public MainStage()
        {
        }
        
        public void Enter()
        {
        }

        public void Leave()
        {
            LogicCenter.Instance.Dispose();
        }

        public StageCode Tick(float deltaTime)
        {
            LogicCenter.Instance.Tick(deltaTime);
            
            return StageCode.Running;
        }
    }
}