namespace LiteQuark.Runtime
{
    internal interface IBundleLocater
    {
        LoadVersionPackTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback);
        ILoadBundleTask LoadBundle(string bundlePath, string hash, System.Action<UnityEngine.AssetBundle> callback);
    }
}