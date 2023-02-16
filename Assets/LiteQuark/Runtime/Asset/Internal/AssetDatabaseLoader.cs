#if UNITY_EDITOR
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
            callback?.Invoke(LoadAssetSync<T>(assetPath));
        }

        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var fullPath = PathUtils.GetFullPathInAssetRoot(assetPath);
            var asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
            {
                LLog.Error($"can't load asset : {fullPath}");
            }
            return asset;
        }

        public void InstantiateAsync(string assetPath, Action<UnityEngine.GameObject> callback)
        {
            LoadAssetAsync<UnityEngine.GameObject>(assetPath, (asset) =>
            {
                var instance = UnityEngine.Object.Instantiate(asset);
                callback?.Invoke(instance);
            });
        }

        public UnityEngine.GameObject InstantiateSync(string assetPath)
        {
            var asset = LoadAssetSync<UnityEngine.GameObject>(assetPath);
            var instance = UnityEngine.Object.Instantiate(asset);
            return instance;
        }

        public void UnloadAsset(string assetPath)
        {
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset is UnityEngine.GameObject)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        public void UnloadUnusedBundle()
        {
            UnityEngine.Resources.UnloadUnusedAssets();
        }
    }
}
#endif