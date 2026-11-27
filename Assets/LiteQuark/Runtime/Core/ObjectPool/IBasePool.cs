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

    public interface IGameObjectPool : IBasePool
    {
        void Alloc(System.Action<GameObjectLease> callback);
        void Alloc(Transform parent, System.Action<GameObjectLease> callback);
        UniTask<GameObjectLease> Alloc();
        UniTask<GameObjectLease> Alloc(Transform parent);
    }
}
