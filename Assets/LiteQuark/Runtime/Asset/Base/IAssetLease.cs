namespace LiteQuark.Runtime
{
    internal interface IAssetLease<T> : IDispose
    {
        T Value { get; }
    }
}
