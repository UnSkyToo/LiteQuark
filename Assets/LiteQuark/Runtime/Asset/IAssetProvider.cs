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
#if LITE_QUARK_ASSET_ENABLE_SYNC
        T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object;
#endif
        
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback);
#if LITE_QUARK_ASSET_ENABLE_SYNC
        UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent);
#endif

        void LoadSceneAsync(string scenePath, string sceneName, LoadSceneParameters parameters, Action<bool> callback);
#if LITE_QUARK_ASSET_ENABLE_SYNC
        bool LoadSceneSync(string scenePath, string sceneName, LoadSceneParameters parameters);
#endif
        
        void UnloadAsset(string assetPath);
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
        void UnloadSceneAsync(string scenePath, Action callback);
        void UnloadUnusedAssets(int maxDepth);
    }
}