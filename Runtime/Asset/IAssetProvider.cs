using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    public interface IAssetProvider : ITick, IDispose
    {
        Task<bool> Initialize();

        void PreloadBundle(string bundlePath, Action<bool> callback);
        void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object;
        
        void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback);
        void LoadSceneAsync(string scenePath, string sceneName, LoadSceneParameters parameters, Action<bool> callback);
        
        void UnloadAsset(string assetPath);
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
        void UnloadSceneAsync(string scenePath, Action callback);
        void UnloadUnusedAssets(int maxDepth);
    }
}