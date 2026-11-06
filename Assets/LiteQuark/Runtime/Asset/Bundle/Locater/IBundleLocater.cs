namespace LiteQuark.Runtime
{
    internal interface IBundleLocater
    {
        LoadVersionPackTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback);
        // 成功结果只交付一次；回调接收方负责 Dispose。
        ILoadBundleTask LoadBundle(string bundlePath, string hash, System.Action<IBundleResource> callback);
    }
}
