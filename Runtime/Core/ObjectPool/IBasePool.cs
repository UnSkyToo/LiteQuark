using UnityEngine;

namespace LiteQuark.Runtime
{
    public interface IBasePool : IDispose
    {
        string Key { get; }
        string Name { get; }
        int CountAll { get; }
        int CountActive { get; }
        int CountInactive { get; }
        
        void Initialize(string key, params object[] args);
        void Generate(int count);
        void GenerateAsync(int count, System.Action<IBasePool> callback);
    }

    public interface IObjectPool<T> : IBasePool
    {
        T Alloc();
        void Recycle(T value);
    }
    
    public interface IGameObjectPool : IObjectPool<GameObject>
    {
        GameObject Alloc(Transform parent);
    }
}