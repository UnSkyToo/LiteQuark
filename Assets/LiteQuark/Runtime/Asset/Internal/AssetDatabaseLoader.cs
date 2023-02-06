﻿#if UNITY_EDITOR
using System;
using UnityEditor;

namespace LiteQuark.Runtime
{
    internal sealed class AssetDatabaseLoader : IAssetLoader
    {
        public AssetDatabaseLoader()
        {
        }

        public bool Initialize()
        {
            return true;
        }

        public void Dispose()
        {
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            callback?.Invoke(true);
        }

        public void StopLoadAsset(string assetPath)
        {
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var fullPath = PathUtils.GetFullPathInAssetRoot(assetPath);
            var asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
            {
                LLog.Error($"can't load asset : {fullPath}");
            }
            callback?.Invoke(asset);
        }

        public void UnloadAsset(string assetPath)
        {
        }
    }
}
#endif