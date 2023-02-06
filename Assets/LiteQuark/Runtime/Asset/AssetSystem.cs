using System;

namespace LiteQuark.Runtime
{
    public sealed class AssetSystem : ISystem
    {
        private IAssetLoader Loader_ = null;

        public AssetSystem()
        {
#if UNITY_EDITOR
            var mode = LiteRuntime.Instance.Launcher.AssetMode;
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
            Loader_?.Dispose();
            Loader_ = null;
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

        public void StopLoadAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.StopLoadAsset(formatPath);
        }

        public void LoadAsset<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            Loader_?.LoadAssetAsync<T>(formatPath, callback);
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

        public void LoadGameObject(string assetPath, Action<UnityEngine.GameObject> callback)
        {
            LoadAsset<UnityEngine.GameObject>(assetPath, (go) =>
            {
                var instance = UnityEngine.GameObject.Instantiate(go);
                callback?.Invoke(instance);
            });
        }

        public void UnloadGameObject(UnityEngine.GameObject asset)
        {
            UnloadAsset(asset);
            UnityEngine.GameObject.DestroyImmediate(asset);
        }
    }
}