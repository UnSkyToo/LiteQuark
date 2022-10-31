using System;

namespace LiteQuark.Runtime
{
    public sealed class AssetManager : Singleton<AssetManager>, IManager
    {
        private IAssetLoader Loader_ = null;

        public bool Startup()
        {
// #if UNITY_EDITOR
//             Loader_ = new AssetDatabaseLoader();
// #else
//             Loader_ = new AssetBundleLoader();
// #endif
            Loader_ = new AssetBundleLoader();
            // Loader_ = new AssetDatabaseLoader();
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
            Loader_?.LoadAsset<T>(formatPath, callback);
        }
    }
}