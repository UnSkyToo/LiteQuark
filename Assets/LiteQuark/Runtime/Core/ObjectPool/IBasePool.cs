using Cysharp.Threading.Tasks;
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
        bool IsReady { get; }

        void Initialize(string key, params object[] args);
        UniTask<bool> GenerateAsync(int count);
        UniTask<bool> WaitReadyAsync();
    }

    public interface IObjectPool<T> : IBasePool
    {
        void Alloc(System.Action<T> callback);
        UniTask<T> Alloc();
        void Recycle(T value);
    }

    public interface IGameObjectPool : IObjectPool<GameObject>
    {
        void Alloc(Transform parent, System.Action<GameObject> callback);
        UniTask<GameObject> Alloc(Transform parent);
    }
}
