using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IAssetProvider : ITick, IDispose
    {
        UniTask<bool> Initialize();
        string GetVersion();
        
        void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object;
        
        void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback);
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, Action<UnityEngine.GameObject> callback);
        void LoadSceneAsync(string scenePath, string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback);
        
        void UnloadAsset(string assetPath);
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
        void UnloadSceneAsync(string scenePath, string sceneName, Action callback);
        void UnloadUnusedAssets(int maxDepth);
    }
}