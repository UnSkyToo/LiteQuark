namespace LiteQuark.Runtime
{
    internal sealed class ErrorStage : IStage
    {
        public ErrorStage()
        {
        }

        public void Enter()
        {
            LLog.Error("Enter <ErrorStage>, please check log.");
            LiteRuntime.Event.Send(new FrameworkErrorEvent(FrameworkErrorCode.Startup, "System Startup error"));
        }

        public void Leave()
        {
        }

        public StageCode Tick(float deltaTime)
        {
            return StageCode.Running;
        }
    }
}