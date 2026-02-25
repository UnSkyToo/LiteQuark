using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IAssetHandle<T> : IDispose
    {
        public bool IsDone { get; }
        public T Result { get; }
        public UniTask<T> Task { get; }
        public UniTask<T>.Awaiter GetAwaiter();
    }
}