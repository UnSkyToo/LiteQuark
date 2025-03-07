using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    public interface IAssetLoader : ITick, IDispose
    {
        Task<bool> Initialize();

        void PreloadBundle(string bundlePath, Action<bool> callback);
        void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object;
        
        void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
        T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object;
        
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback);
        UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent);

        void LoadSceneAsync(string scenePath, string sceneName, LoadSceneParameters parameters, Action<bool> callback);
        bool LoadSceneSync(string scenePath, string sceneName, LoadSceneParameters parameters);
        
        void UnloadAsset(string assetPath);
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
        void UnloadSceneAsync(string scenePath, Action callback);
        void UnloadUnusedAssets(int maxDepth);
    }
}