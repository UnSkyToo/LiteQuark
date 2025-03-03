namespace LiteQuark.Runtime
{
    internal enum StageCode : byte
    {
        Waiting,
        Running,
        Completed,
        Error,
    }
    
    internal interface IStage
    {
        void Enter();
        void Leave();
        StageCode Tick(float deltaTime);
    }
}