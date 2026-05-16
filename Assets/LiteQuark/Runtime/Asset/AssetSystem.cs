using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class AssetSystem : ISystem, ITick
    {
        private IAssetProvider _provider = null;
        private readonly HashSet<string> _loadingSceneSet = new(StringComparer.OrdinalIgnoreCase);

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
            _loadingSceneSet.Clear();
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
            return PathUtils.FormatAssetPath(path);
        }
        
        public bool HasAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath);
            return _provider?.HasAsset(formatPath) ?? false;
        }

        public AssetHandle<T> LoadAssetHandle<T>(string assetPath, CancellationToken ct = default) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            return new AssetHandle<T>((cb) => _provider?.LoadAssetAsync<T>(formatPath, cb), ct,
                () => ReleaseAssetReference(formatPath));
        }

        public PreloadHandle<T> PreloadAssetHandle<T>(IEnumerable<string> assetPaths, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (assetPaths == null)
            {
                throw new ArgumentNullException(nameof(assetPaths));
            }

            var formatPaths = new List<string>();
            var pathSet = new HashSet<string>();
            foreach (var assetPath in assetPaths)
            {
                var formatPath = FormatPath(assetPath);
                if (pathSet.Add(formatPath))
                {
                    formatPaths.Add(formatPath);
                }
            }

            return new PreloadHandle<T>(formatPaths, LoadAssetHandle<T>, ct);
        }

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, ct),
                (asset) => UnityEngine.Object.Instantiate(asset, parent), ct);

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, ct),
                (asset) => UnityEngine.Object.Instantiate(asset, position, rotation, parent), ct);

        private void LoadSceneInternal(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool, UnityEngine.SceneManagement.Scene> callback)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            if (!_loadingSceneSet.Add(sceneName))
            {
                LLog.Error("Scene is already loading: {0}", sceneName);
                LiteUtils.SafeInvoke(callback, false, default);
                return;
            }

            var formatPath = FormatPath(scenePath);
            if (_provider == null)
            {
                _loadingSceneSet.Remove(sceneName);
                LiteUtils.SafeInvoke(callback, false, default);
                return;
            }

            try
            {
                _provider.LoadSceneAsync(formatPath, sceneName, parameters, (success, scene) =>
                {
                    _loadingSceneSet.Remove(sceneName);
                    LiteUtils.SafeInvoke(callback, success, scene);
                });
            }
            catch
            {
                _loadingSceneSet.Remove(sceneName);
                throw;
            }
        }

        public SceneHandle LoadSceneHandle(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, CancellationToken ct = default)
            => new SceneHandle((cb) => LoadSceneInternal(scenePath, parameters, cb), ct,
                (scene) => ReleaseSceneReferenceAsync(scenePath, scene, null));

        private void ReleaseAssetReference(string assetPath)
        {
            _provider?.ReleaseAssetReference(assetPath);
        }

        private void ReleaseSceneReferenceAsync(string scenePath, UnityEngine.SceneManagement.Scene scene, Action callback)
        {
            var sceneName = PathUtils.GetFileNameWithoutExt(scenePath);
            var formatPath = FormatPath(scenePath);
            _provider?.ReleaseSceneReferenceAsync(formatPath, sceneName, scene, callback);
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

            return new VisitorInfo(null, Array.Empty<BundleVisitorInfo>());
        }
#endif
    }
}
