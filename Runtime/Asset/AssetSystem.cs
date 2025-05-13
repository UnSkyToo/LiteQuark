using System;
using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class AssetSystem : ISystem, ITick
    {
        private IAssetProvider _provider = null;

        public AssetSystem()
        {
#if UNITY_EDITOR
            var mode = LiteRuntime.Setting.Asset.AssetMode;
#else
            var mode = AssetProviderMode.Bundle;
#endif
            switch (mode)
            {
#if UNITY_EDITOR
                case AssetProviderMode.Internal:
                    _provider = new AssetDatabaseProvider();
                    break;
#endif
                case AssetProviderMode.Bundle:
                    _provider = new AssetBundleProvider();
                    break;
                default:
                    throw new ArgumentException($"error {nameof(AssetProviderMode)} : {mode}");
            }
        }
        
        public Task<bool> Initialize()
        {
            return _provider.Initialize();
        }

        public void Dispose()
        {
            _provider?.UnloadUnusedAssets(20);
            
            _provider?.Dispose();
            _provider = null;
        }

        public void Tick(float deltaTime)
        {
            _provider?.Tick(deltaTime);
        }

        private string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            
            return path.TrimStart('/').ToLower();
        }

        public void PreloadBundle(string bundlePath, Action<bool> callback)
        {
            var formatPath = FormatPath(bundlePath);
            _provider?.PreloadBundle(formatPath, callback);
        }

        public Task<bool> PreloadBundle(string bundlePath)
        {
            var tcs = new TaskCompletionSource<bool>();
            PreloadBundle(bundlePath, (result) =>
            {
                tcs.SetResult(result);
            });
            return tcs.Task;
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            _provider?.PreloadAsset<T>(formatPath, callback);
        }

        public Task<bool> PreloadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            var tcs = new TaskCompletionSource<bool>();
            PreloadAsset<T>(assetPath, (result) =>
            {
                tcs.SetResult(result);
            });
            return tcs.Task;
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            _provider?.LoadAssetAsync<T>(formatPath, callback);
        }
        
        public Task<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            var tcs = new TaskCompletionSource<T>();
            LoadAssetAsync<T>(assetPath, (asset) =>
            {
                tcs.SetResult(asset);
            });
            return tcs.Task;
        }
        
        // public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        // {
        //     var formatPath = FormatPath(assetPath);
        //     return Provider_?.LoadAssetSync<T>(formatPath);
        // }

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            var formatPath = FormatPath(assetPath);
            _provider?.InstantiateAsync(formatPath, parent, callback);
        }
        
        public Task<UnityEngine.GameObject> InstantiateAsync(string assetPath, UnityEngine.Transform parent)
        {
            var tcs = new TaskCompletionSource<UnityEngine.GameObject>();
            InstantiateAsync(assetPath, parent, (gameObject) =>
            {
                tcs.SetResult(gameObject);
            });
            return tcs.Task;
        }
        
        // public UnityEngine.GameObject InstantiateSync(string assetPath, UnityEngine.Transform parent)
        // {
        //     var formatPath = FormatPath(assetPath);
        //     return Provider_?.InstantiateSync(formatPath, parent);
        // }
        
        public void LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            var formatPath = FormatPath(scenePath);
            _provider?.LoadSceneAsync(formatPath, sceneName, parameters, callback);
        }
        
        public Task<bool> LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters)
        {
            var tcs = new TaskCompletionSource<bool>();
            LoadSceneAsync(scenePath, parameters, (result) =>
            {
                tcs.SetResult(result);
            });
            return tcs.Task;
        }
        
        // public bool LoadSceneSync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters)
        // {
        //     var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
        //     var formatPath = FormatPath(scenePath);
        //     return Provider_?.LoadSceneSync(formatPath, sceneName, parameters) ?? false;
        // }

        public void UnloadAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath);
            _provider?.UnloadAsset(formatPath);
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            _provider?.UnloadAsset(asset);
        }
        
        public void UnloadSceneAsync(string scenePath, Action callback)
        {
            var formatPath = FormatPath(scenePath);
            _provider?.UnloadSceneAsync(formatPath, callback);
        }
        
        public Task UnloadSceneAsync(string scenePath)
        {
            var tcs = new TaskCompletionSource<bool>();
            UnloadSceneAsync(scenePath, () =>
            {
                tcs.SetResult(true);
            });
            return tcs.Task;
        }

        /// <summary>
        /// 释放未使用的资源（包括处于Retain的缓存资源），可以在需要的时候调用
        /// </summary>
        /// <param name="maxDepth">循环释放嵌套引用的最大层数。例如：A->B->C，如果为2，则只释放到B这一层</param>
        public void UnloadUnusedAssets(int maxDepth = 5)
        {
            _provider?.UnloadUnusedAssets(maxDepth);
        }
        
#if UNITY_EDITOR
        public VisitorInfo GetVisitorInfo()
        {
            if (_provider is AssetBundleProvider provider)
            {
                return provider.GetVisitorInfo();
            }

            return new VisitorInfo(null);
        }
#endif
    }
}