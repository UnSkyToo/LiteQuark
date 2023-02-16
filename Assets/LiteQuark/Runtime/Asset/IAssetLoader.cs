﻿using System;

namespace LiteQuark.Runtime
{
    public interface IAssetLoader : IDispose
    {
        bool Initialize();

        void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object;
        void StopLoadAsset(string assetPath);
        
        void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object;
        T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object;
        void InstantiateAsync(string assetPath, Action<UnityEngine.GameObject> callback);
        UnityEngine.GameObject InstantiateSync(string assetPath);

        void UnloadAsset(string assetPath);
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
        void UnloadUnusedBundle();
    }
}