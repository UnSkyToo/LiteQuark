using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Owns one resource lifetime. Dispose releases the ownership held by this handle.
    /// </summary>
    public interface IAssetHandle<T> : IDispose
    {
        public bool IsDone { get; }
        public T Result { get; }
        public UniTask<T> Task { get; }
        public UniTask<T>.Awaiter GetAwaiter();
    }
}
