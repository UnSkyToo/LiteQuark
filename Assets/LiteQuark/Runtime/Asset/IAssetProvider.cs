using System;

namespace LiteQuark.Runtime
{
    internal interface IAssetProvider : IInitializeAsync, ITick, IDispose
    {
        string GetVersion();

        bool HasAsset(string assetPath);

        void LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object;
        void LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority, Action<SceneInfoCache.Lease> callback);

        void UnloadUnusedAssets(int maxDepth);
    }
}
