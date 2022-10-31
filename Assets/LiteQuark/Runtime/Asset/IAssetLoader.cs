using System;

namespace LiteQuark.Runtime
{
    public interface IAssetLoader : IDisposable
    {
        bool Initialize();

        void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object;
        
        void LoadAsset<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
    }
}