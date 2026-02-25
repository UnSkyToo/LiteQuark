using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
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
            
            return path.TrimStart('/').ToLowerInvariant();
        }
        
        public bool HasAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath);
            return _provider?.HasAsset(formatPath) ?? false;
        }

        public void PreloadAsset<T>(string assetPath, Action<bool> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            _provider?.PreloadAsset<T>(formatPath, callback);
        }

        public UniTask<bool> PreloadAsset<T>(string assetPath, CancellationToken ct = default) where T : UnityEngine.Object
            => CallbackToUniTask<bool>((cb) => PreloadAsset<T>(assetPath, cb), ct,
                (success) => { if (success) UnloadAsset(assetPath); });

        public void LoadAssetAsync<T>(string assetPath, Action<T> callback) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            _provider?.LoadAssetAsync<T>(formatPath, callback);
        }
        
        public UniTask<T> LoadAssetAsync<T>(string assetPath, CancellationToken ct = default) where T : UnityEngine.Object
            => CallbackToUniTask<T>((cb) => LoadAssetAsync<T>(assetPath, cb), ct,
                (asset) => { if (asset != null) UnloadAsset(asset); });
        
        public AssetHandle<T> LoadAssetHandle<T>(string assetPath, CancellationToken ct = default) where T : UnityEngine.Object
            => new AssetHandle<T>((cb) => LoadAssetAsync<T>(assetPath, cb), ct, UnloadAsset);

        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, Action<UnityEngine.GameObject> callback)
        {
            var formatPath = FormatPath(assetPath);
            _provider?.InstantiateAsync(formatPath, parent, callback);
        }
        
        public UniTask<UnityEngine.GameObject> InstantiateAsync(string assetPath, UnityEngine.Transform parent, CancellationToken ct = default)
            => CallbackToUniTask<UnityEngine.GameObject>(
                (cb) => InstantiateAsync(assetPath, parent, cb), ct,
                (go) => { if (go != null) UnloadAsset(go); });
        
        public AssetHandle<UnityEngine.GameObject> InstantiateHandle(string assetPath, UnityEngine.Transform parent, CancellationToken ct = default)
            => new AssetHandle<UnityEngine.GameObject>((cb) => InstantiateAsync(assetPath, parent, cb), ct, UnloadAsset);
        
        public void InstantiateAsync(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, Action<UnityEngine.GameObject> callback)
        {
            var formatPath = FormatPath(assetPath);
            _provider?.InstantiateAsync(formatPath, parent, position, rotation, callback);
        }
        
        public UniTask<UnityEngine.GameObject> InstantiateAsync(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, CancellationToken ct = default)
            => CallbackToUniTask<UnityEngine.GameObject>(
                (cb) => InstantiateAsync(assetPath, parent, position, rotation, cb), ct,
                (go) => { if (go != null) UnloadAsset(go); });
        
        public AssetHandle<UnityEngine.GameObject> InstantiateHandle(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, CancellationToken ct = default)
            => new AssetHandle<UnityEngine.GameObject>((cb) => InstantiateAsync(assetPath, parent, position, rotation, cb), ct, UnloadAsset);
        
        public void LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            var formatPath = FormatPath(scenePath);
            _provider?.LoadSceneAsync(formatPath, sceneName, parameters, callback);
        }
        
        public UniTask<bool> LoadSceneAsync(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, CancellationToken ct = default)
            => CallbackToUniTask<bool>((cb) => LoadSceneAsync(scenePath, parameters, cb), ct);
        
        public SceneHandle LoadSceneHandle(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, CancellationToken ct = default)
            => new SceneHandle(scenePath, parameters, ct);

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
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            var formatPath = FormatPath(scenePath);
            _provider?.UnloadSceneAsync(formatPath, sceneName, callback);
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
        
        private UniTask<T> CallbackToUniTask<T>(Action<Action<T>> invoke, CancellationToken ct, Action<T> onCancelled = null)
        {
            var tcs = new UniTaskCompletionSource<T>();

            if (ct.IsCancellationRequested)
            {
                tcs.TrySetCanceled(ct);
                return tcs.Task;
            }

            CancellationTokenRegistration ctr = default;
            if (ct.CanBeCanceled)
            {
                ctr = ct.Register(() => tcs.TrySetCanceled(ct));
            }

            invoke((result) =>
            {
                ctr.Dispose();
                if (!tcs.TrySetResult(result))
                {
                    onCancelled?.Invoke(result);
                }
            });
            return tcs.Task;
        }
        
#if UNITY_EDITOR
        internal VisitorInfo GetVisitorInfo()
        {
            if (_provider is AssetBundleProvider provider)
            {
                return provider.GetVisitorInfo();
            }

            return new VisitorInfo(null, Array.Empty<BundleVisitorInfo>());
        }
#endif
    }
}