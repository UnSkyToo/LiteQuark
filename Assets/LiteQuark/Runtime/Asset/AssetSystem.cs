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
        private IAssetManager _manager = null;

        public AssetSystem()
        {
            _manager = new AssetManager();
        }

        public UniTask<bool> Initialize()
        {
            var concurrencyLimit = Math.Max(1, LiteRuntime.Setting.Asset.TaskConcurrencyLimit);
            LiteRuntime.Task.RegisterGroup(LiteConst.TaskGroupId.Asset, concurrencyLimit);

            IBundleProvider bundleProvider;
            try
            {
                bundleProvider = CreateBundleProvider(UnityEngine.Application.isEditor);
            }
            catch (Exception ex)
            {
                // 后端创建失败通过初始化任务交付；任务组注册保留同步异常语义。
                return UniTask.FromException<bool>(ex);
            }

            return _manager.Initialize(bundleProvider);
        }

        public async UniTask DisposeAsync()
        {
            if (_manager == null)
            {
                return;
            }
            await _manager.DisposeAsync();
            _manager = null;
        }

        public void Tick(float deltaTime)
        {
            _manager?.Tick(LiteTime.UnscaledDeltaTime);
        }

        public string GetVersion()
        {
            return _manager?.GetVersion();
        }

        public bool HasAsset(string assetPath)
        {
            if (!AssetPath.TryResolve(assetPath, out var assetKey))
            {
                return false;
            }

            return _manager?.HasAsset(assetKey) ?? false;
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
                if (_manager == null)
                {
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(
                        new AssetError(assetKey, "Asset manager is unavailable")));
                    return;
                }

                _manager.LoadAssetAsync(assetKey, priority, callback);
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
                if (_manager == null)
                {
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(
                        new AssetError(sceneKey, "Asset manager is unavailable")));
                    return;
                }

                _manager?.LoadSceneAsync(sceneKey, parameters, priority, callback);
            }, ct);
        }

        /// <summary>
        /// 释放未使用的资源（包括处于Retain的缓存资源），可以在需要的时候调用
        /// </summary>
        /// <param name="maxDepth">释放引用的最大层数。例如：A->B->C，如果为2，则只释放到B这一层</param>
        public void UnloadUnusedAssets(int maxDepth = 5)
        {
            _manager?.UnloadUnusedAssets(maxDepth);
        }

        private IBundleProvider CreateBundleProvider(bool isEditor)
        {
            var mode = LiteRuntime.Setting.Asset.BundleMode;
            // 默认模拟配置也可用于 Player；只有编辑器能访问项目资源。
            if (!isEditor && mode == BundleLoadMode.Simulate)
            {
                mode = BundleLoadMode.BuiltIn;
            }
            switch (mode)
            {
                case BundleLoadMode.BuiltIn:
                    return new BuiltInBundleProvider
                    {
                        EditorForceStreamingAssets = LiteRuntime.Setting.Asset.EditorForceStreamingAssets
                    };
                case BundleLoadMode.Remote:
                    return new RemoteBundleProvider(PathUtils.ConcatUri(
                        LiteRuntime.Setting.Asset.BundleRemoteUri,
                        AppUtils.GetCurrentPlatformName().ToLowerInvariant(),
                        AppUtils.GetMainVersion()))
                    {
                        DisableUnityWebCache = LiteRuntime.Setting.Asset.DisableUnityWebCache
                    };
#if UNITY_EDITOR
                case BundleLoadMode.Simulate:
                    return new SimulateBundleProvider();
#endif
                default:
                    throw new ArgumentException($"error {nameof(BundleLoadMode)} : {mode}");
            }
        }

#if UNITY_EDITOR
        internal VisitorInfo GetVisitorInfo()
        {
            if (_manager is AssetManager manager)
            {
                return manager.GetVisitorInfo();
            }

            return new VisitorInfo(null, Array.Empty<BundleVisitorInfo>());
        }
#endif
    }
}
