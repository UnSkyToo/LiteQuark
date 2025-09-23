using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal interface IBundleLocater
    {
        UniTask<VersionPackInfo> LoadVersionPack(string versionFileName);
        LoadBundleBaseTask LoadBundle(string bundlePath, System.Action<UnityEngine.AssetBundle> callback);
    }
}