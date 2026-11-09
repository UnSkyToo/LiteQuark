namespace LiteQuark.Runtime
{
    internal interface IBundleProvider
    {
        BaseTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback);
        // 成功结果只交付一次；回调接收方负责 Dispose。
        BaseTask LoadBundle(string bundlePath, string hash, System.Action<IBundleResource> callback);
    }
}
