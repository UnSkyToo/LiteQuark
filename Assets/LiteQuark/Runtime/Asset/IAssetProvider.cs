using System;

namespace LiteQuark.Runtime
{
    internal interface IAssetProvider : IInitializeAsync, ITick, IDispose
    {
        string GetVersion();

        bool HasAsset(string assetPath);
        
        void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback);
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, Action<UnityEngine.GameObject> callback);
        void LoadSceneAsync(string scenePath, string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback);
        
        void ReleaseAssetReference(string assetPath);
        void ReleaseSceneReferenceAsync(string scenePath, string sceneName, Action callback);
        void UnloadUnusedAssets(int maxDepth);
    }
}
