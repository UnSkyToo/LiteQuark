using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class AssetSystem : ISystem, ITick
    {
        private IAssetProvider _provider = null;

        public AssetSystem()
        {
            var mode = LiteRuntime.Setting.Asset.AssetMode;
            
#if !UNITY_EDITOR
            if (mode == AssetProviderMode.Editor)
            {
                mode = AssetProviderMode.Bundle;
            }
#endif
            switch (mode)
            {
#if UNITY_EDITOR
                case AssetProviderMode.Editor:
                    _provider = new AssetEditorProvider();
                    break;
#endif
                case AssetProviderMode.Bundle:
                    _provider = new AssetBundleProvider();
                    break;
                case AssetProviderMode.Resource:
                    _provider = new AssetResourceProvider();
                    break;
                default:
                    throw new ArgumentException($"error {nameof(AssetProviderMode)} : {mode}");
            }
        }
        
        public UniTask<bool> Initialize()
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

        public string GetVersion()
        {
            return _provider?.GetVersion();
        }

        private string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            
            return path.TrimStart('/').ToLower();
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            _provider?.PreloadAsset<T>(formatPath, callback);
        }

        public UniTask<bool> PreloadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            var tcs = new UniTaskCompletionSource<bool>();
            PreloadAsset<T>(assetPath, (result) =>
            {
                tcs.TrySetResult(result);
            });
            return tcs.Task;
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            _provider?.LoadAssetAsync<T>(formatPath, callback);
        }
        
        public UniTask<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object
        {
            var tcs = new UniTaskCompletionSource<T>();
            LoadAssetAsync<T>(assetPath, (asset) =>
            {
                tcs.TrySetResult(asset);
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
        
        public UniTask<UnityEngine.GameObject> InstantiateAsync(string assetPath, UnityEngine.Transform parent)
        {
            var tcs = new UniTaskCompletionSource<UnityEngine.GameObject>();
            InstantiateAsync(assetPath, parent, (gameObject) =>
            {
                tcs.TrySetResult(gameObject);
            });
            return tcs.Task;
        }
        
        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, Action<UnityEngine.GameObject> callback)
        {
            var formatPath = FormatPath(assetPath);
            _provider?.InstantiateAsync(formatPath, parent, position, rotation, callback);
        }
        
        public UniTask<UnityEngine.GameObject> InstantiateAsync(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
        {
            var tcs = new UniTaskCompletionSource<UnityEngine.GameObject>();
            InstantiateAsync(assetPath, parent, position, rotation, (gameObject) =>
            {
                tcs.TrySetResult(gameObject);
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
            var formatPath = FormatPath(scenePath);
            _provider?.LoadSceneAsync(formatPath, parameters, callback);
        }
        
        public UniTask<bool> LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            LoadSceneAsync(scenePath, parameters, (result) =>
            {
                tcs.TrySetResult(result);
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
        
        public UniTask UnloadSceneAsync(string scenePath)
        {
            var tcs = new UniTaskCompletionSource();
            UnloadSceneAsync(scenePath, () =>
            {
                tcs.TrySetResult();
            });
            return tcs.Task;
        }

        /// <summary>
        /// 释放未使用的资源（包括处于Retain的缓存资源），可以在需要的时候调用
        /// </summary>
        /// <param name="maxDepth">释放引用的最大层数。例如：A->B->C，如果为2，则只释放到B这一层</param>
        public void UnloadUnusedAssets(int maxDepth = 5)
        {
            _provider?.UnloadUnusedAssets(maxDepth);
        }
        
#if UNITY_EDITOR
        internal VisitorInfo GetVisitorInfo()
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