namespace LiteQuark.Runtime
{
    public interface IObjectPool
    {
        void Initialize(object param);
        void Clean();
    }
}