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
            var concurrencyLimit = Math.Max(1, LiteRuntime.Setting.Asset.TaskConcurrencyLimit);
            LiteRuntime.Task.RegisterGroup(LiteConst.TaskGroupId.Asset, concurrencyLimit);

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
            return PathUtils.FormatAssetPath(path);
        }

        public bool HasAsset(string assetPath)
        {
            var formatPath = FormatPath(assetPath);
            return _provider?.HasAsset(formatPath) ?? false;
        }

        public AssetHandle<T> LoadAssetHandle<T>(string assetPath, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default) where T : UnityEngine.Object
        {
            var formatPath = FormatPath(assetPath);
            return new AssetHandle<T>(cb => _provider?.LoadAssetAsync(formatPath, priority, cb), ct);
        }

        public PreloadHandle<T> PreloadAssetHandle<T>(IEnumerable<string> assetPaths, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default) where T : UnityEngine.Object
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

            return new PreloadHandle<T>(formatPaths, (path, token) => LoadAssetHandle<T>(path, priority, token), ct);
        }

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, priority, ct),
                asset => UnityEngine.Object.Instantiate(asset, parent), ct);

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, priority, ct),
                asset => UnityEngine.Object.Instantiate(asset, position, rotation, parent), ct);

        public SceneHandle LoadSceneHandle(string scenePath, UnityEngine.SceneManagement.LoadSceneParameters parameters, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
            => new SceneHandle(cb => _provider?.LoadSceneAsync(scenePath, parameters, priority, cb), ct);

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
