namespace LiteQuark.Runtime
{
    /// <summary>
    /// Owns one resource lifetime. Dispose releases the ownership held by this handle.
    /// </summary>
    public interface IAssetHandle<T> : IDispose
    {
        public AssetHandleState State { get; }
        public AssetError Error { get; }
        public bool IsDone { get; }
        public bool IsDisposed { get; }
        public T Result { get; }
    }
}
