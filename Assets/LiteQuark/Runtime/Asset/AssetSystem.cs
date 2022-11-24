using System;

namespace LiteQuark.Runtime
{
    public sealed class AssetSystem : IDispose
    {
        private IAssetLoader Loader_ = null;

        public AssetSystem(AssetLoaderMode mode)
        {
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
            return path.TrimStart('/');
        }

        public void LoadAsset<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath).ToLower();
            Loader_?.LoadAssetAsync<T>(formatPath, callback);
        }

        public void UnloadAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath).ToLower();
            Loader_?.UnloadAsset(assetPath);
        }
    }
}