namespace LiteQuark.Runtime
{
    // Loader 持有成功结果，负责交付给 Cache 或在关闭时释放。
    internal interface ILoadBundleTask : ITask
    {
        IBundleResource GetBundle();
    }
}
