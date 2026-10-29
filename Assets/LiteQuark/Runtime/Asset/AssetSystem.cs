using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

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

        public async UniTask DisposeAsync()
        {
            if (_provider == null)
            {
                return;
            }
            await _provider.DisposeAsync();
            _provider = null;
        }

        public void Tick(float deltaTime)
        {
            _provider?.Tick(LiteTime.UnscaledDeltaTime);
        }

        public string GetVersion()
        {
            return _provider?.GetVersion();
        }

        public bool HasAsset(string assetPath)
        {
            if (!AssetPath.TryResolve(assetPath, out var assetKey))
            {
                return false;
            }

            return _provider?.HasAsset(assetKey) ?? false;
        }

        public AssetHandle<T> LoadAssetHandle<T>(string assetPath, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default) where T : UnityEngine.Object
        {
            var assetKey = AssetPath.Resolve(assetPath);
            return LoadAssetHandleInternal<T>(assetKey, priority, ct);
        }

        private AssetHandle<T> LoadAssetHandleInternal<T>(
            string assetKey,
            AssetLoadPriority priority,
            CancellationToken ct)
            where T : UnityEngine.Object
        {
            return new AssetHandle<T>(assetKey, callback =>
            {
                if (_provider == null)
                {
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(
                        new AssetError(assetKey, "Asset provider is unavailable")));
                    return;
                }

                _provider.LoadAssetAsync(assetKey, priority, callback);
            }, ct);
        }

        public PreloadHandle<T> PreloadAssetHandle<T>(IEnumerable<string> assetPaths, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (assetPaths == null)
            {
                throw new ArgumentNullException(nameof(assetPaths));
            }

            var assetKeys = new List<string>();
            var keySet = new HashSet<string>(StringComparer.Ordinal);

            foreach (var assetPath in assetPaths)
            {
                var assetKey = AssetPath.Resolve(assetPath);
                if (keySet.Add(assetKey))
                {
                    assetKeys.Add(assetKey);
                }
            }

            return new PreloadHandle<T>(assetKeys, (assetKey, token) => LoadAssetHandleInternal<T>(assetKey, priority, token), ct);
        }

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, priority, ct),
                asset => UnityEngine.Object.Instantiate(asset, parent), ct);

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, priority, ct),
                asset => UnityEngine.Object.Instantiate(asset, position, rotation, parent), ct);

        public SceneHandle LoadSceneHandle(string scenePath, LoadSceneParameters parameters, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
        {
            var sceneKey = AssetPath.ResolveScene(scenePath);

            return new SceneHandle(callback =>
            {
                if (_provider == null)
                {
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(
                        new AssetError(sceneKey, "Asset provider is unavailable")));
                    return;
                }

                _provider?.LoadSceneAsync(sceneKey, parameters, priority, callback);
            }, ct);
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
