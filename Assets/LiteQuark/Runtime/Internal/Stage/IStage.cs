namespace LiteQuark.Runtime
{
    internal interface IStage
    {
        void Enter();
        void Leave();
        StageCode Tick(float deltaTime);
    }
}