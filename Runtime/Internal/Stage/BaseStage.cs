namespace LiteQuark.Runtime
{
    internal abstract class BaseStage : IStage
    {
        protected BaseStage()
        {
        }
        
        public abstract void Enter();
        public abstract void Leave();
        public abstract StageCode Tick(float deltaTime);
    }
}