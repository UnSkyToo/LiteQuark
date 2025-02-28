namespace LiteQuark.Runtime
{
    internal sealed class ErrorStage : BaseStage
    {
        public ErrorStage()
        {
        }

        public override void Enter()
        {
            LLog.Error("Enter <ErrorStage>, please check log.");
        }

        public override void Leave()
        {
        }

        public override StageCode Tick(float deltaTime)
        {
            return StageCode.Running;
        }
    }
}