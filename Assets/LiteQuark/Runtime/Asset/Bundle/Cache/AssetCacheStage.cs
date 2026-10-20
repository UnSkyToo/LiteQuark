namespace LiteQuark.Runtime
{
    internal enum AssetCacheStage : int
    {
        Idle,
        Loading,
        Loaded,
        Unloading,
        Unloaded,
    }
}
