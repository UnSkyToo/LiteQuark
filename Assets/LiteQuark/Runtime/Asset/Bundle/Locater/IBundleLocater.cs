namespace LiteQuark.Runtime
{
    internal interface IBundleLocater
    {
        LoadVersionPackTask LoadVersionPack(string versionFileName, System.Action<VersionPackInfo> callback);
        LoadBundleBaseTask LoadBundle(string bundlePath, System.Action<UnityEngine.AssetBundle> callback);
    }
}