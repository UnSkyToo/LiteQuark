using System;

namespace LiteQuark.Runtime
{
    internal interface IAssetProvider : IInitializeAsync, ITick, IDispose
    {
        string GetVersion();

        bool HasAsset(string assetKey);

        void LoadAssetAsync<T>(string assetKey, AssetLoadPriority priority, Action<AssetInfoCache.Lease<T>> callback) where T : UnityEngine.Object;
        void LoadSceneAsync(string sceneKey, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority, Action<SceneInfoCache.Lease> callback);

        void UnloadUnusedAssets(int maxDepth);
    }
}
