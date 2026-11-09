namespace LiteQuark.Runtime
{
    internal interface IBundleProvider
    {
        // 返回已提交到 Asset 任务组的任务，调用方不再提交。
        // 清单成功结果为已 Initialize 的 VersionPackInfo。
        BaseTask LoadVersionPack(string versionFileName);
        // 成功容器由 Loader 接收并管理；关闭时未交付的成功结果也由 Loader 释放。
        // 任务负责取消／失败后的迟到资源，并通过 IResourceTaskCleanup 暴露收尾等待。
        ILoadBundleTask LoadBundle(string bundlePath, string hash);
    }
}
