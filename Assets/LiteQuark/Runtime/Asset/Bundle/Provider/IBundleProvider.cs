namespace LiteQuark.Runtime
{
    internal interface IBundleProvider
    {
        // 返回已提交到 Asset 任务组的任务，调用方不再提交。
        // 清单成功结果为已 Initialize 的 VersionPackInfo。
        BaseTask LoadVersionPack(string versionFileName);
        // 成功容器由 Loader 接收并管理；关闭时未交付的成功结果也由 Loader 释放。
        // 任务自行处理停止请求，就绪后由 Loader 交付或释放容器。
        ILoadBundleTask LoadBundle(string bundlePath, string hash);
    }
}
