using System;

namespace LiteQuark.Runtime
{
    public sealed class AssetManager : Singleton<AssetManager>, IManager
    {
        private IAssetLoader Loader_ = null;

        public bool Startup()
        {
            switch (LiteRuntime.Instance.AssetMode)
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
                    throw new ArgumentException($"error {nameof(AssetLoaderMode)} : {LiteRuntime.Instance.AssetMode}");
            }
            
            return Loader_.Initialize();
        }

        public void Shutdown()
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
    }
}