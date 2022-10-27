namespace LiteQuark.Runtime
{
    public interface IGameLogic : ITick
    {
        bool Startup();
        void Shutdown();
    }
}