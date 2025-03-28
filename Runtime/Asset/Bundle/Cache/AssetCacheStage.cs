namespace LiteQuark.Runtime
{
    internal enum AssetCacheStage : int
    {
        Created,
        Loading,
        Loaded,
        Retained,
        Unloading,
        Unloaded,
        Invalid,
    }
}