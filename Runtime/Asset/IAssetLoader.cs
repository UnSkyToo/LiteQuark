using System;

namespace LiteQuark.Runtime
{
    public interface IAssetLoader : IDisposable
    {
        bool Initialize();
        
        void LoadAsset<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
    }
}