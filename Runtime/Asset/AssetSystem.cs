using System;

namespace LiteQuark.Runtime
{
    public sealed class AssetSystem : ISystem, ITick
    {
        private IAssetLoader Loader_ = null;

        public AssetSystem()
        {
#if UNITY_EDITOR
            var mode = LiteRuntime.Setting.Asset.AssetMode;
#else
            var mode = AssetLoaderMode.Bundle;
#endif
            switch (mode)
            {
#if UNITY_EDITOR
                case AssetLoaderMode.Internal:
                    Loader_ = new AssetDatabaseLoader();
                    break;
#endif
                case AssetLoaderMode.Bundle:
                    Loader_ = new AssetBundleLoader();
                    break;
                default:
                    throw new ArgumentException($"error {nameof(AssetLoaderMode)} : {mode}");
            }
            
            Loader_.Initialize();
        }

        public void Dispose()
        {
            Loader_?.UnloadUnusedAssets();
            
            Loader_?.Dispose();
            Loader_ = null;
        }

        public void Tick(float deltaTime)
        {
            Loader_?.Tick(deltaTime);
        }

        private string FormatPath(string path)
        {
            return path.TrimStart('/').ToLower();
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.PreloadAsset<T>(formatPath, callback);
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.LoadAssetAsync<T>(formatPath, callback);
        }

        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            return Loader_?.LoadAssetSync<T>(formatPath);
        }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.InstantiateAsync(formatPath, parent, callback);
        }

        public UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent)
        {
            var formatPath = FormatPath(assetPath);
            return Loader_?.InstantiateSync(formatPath, parent);
        }

        public void UnloadAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.UnloadAsset(formatPath);
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            Loader_?.UnloadAsset(asset);
        }

        public void UnloadUnusedAssets()
        {
            Loader_?.UnloadUnusedAssets();
        }
        
#if UNITY_EDITOR
        public VisitorInfo GetVisitorInfo()
        {
            if (Loader_ is AssetBundleLoader loader)
            {
                return loader.GetVisitorInfo();
            }

            return new VisitorInfo(null);
        }
#endif
    }
}