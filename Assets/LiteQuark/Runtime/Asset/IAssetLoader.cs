using System;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IAssetLoader : ITick, IDispose
    {
        Task<bool> Initialize();

        void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object;
        
        void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
        T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object;
        void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback);
        UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent);

        void UnloadAsset(string assetPath);
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
        void UnloadUnusedAssets();
    }
}