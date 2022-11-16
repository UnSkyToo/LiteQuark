namespace LiteQuark.Runtime
{
    public interface ILogic : ITick
    {
        bool Startup();
        void Shutdown();
    }
}