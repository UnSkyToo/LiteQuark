namespace LiteQuark.Runtime
{
    // Result、Task 和 GetBundle 观察同一容器，不取得额外所有权。
    // Loader 持有任务，负责成功结果的交付或关闭释放；任务包装器回收不释放成功容器。
    internal interface ILoadBundleTask : ITask
    {
        IBundleResource GetBundle();
    }
}
