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
        private AssetManager _manager = new();

        public UniTask<bool> Initialize(CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
            {
                return UniTask.FromCanceled<bool>(ct);
            }

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

            return _manager.Initialize(bundleProvider, ct);
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
            var manager = _manager;
            return new AssetHandle<T>(assetKey, callback =>
            {
                if (manager == null)
                {
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<T>>.Failure(
                        new AssetError(assetKey, "Asset manager is unavailable")));
                    return;
                }

                manager.LoadAssetAsync(assetKey, priority, callback);
            }, () => manager?.GetLoadProgress(assetKey, typeof(T)) ?? 0f, ct);
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

            return new PreloadHandle<T>(assetKeys, assetKey => LoadAssetHandleInternal<T>(assetKey, priority, CancellationToken.None), ct);
        }

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, priority, ct),
                asset => UnityEngine.Object.Instantiate(asset, parent), ct);

        public GameObjectHandle InstantiateHandle(string assetPath, UnityEngine.Transform parent, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
            => new GameObjectHandle(LoadAssetHandle<UnityEngine.GameObject>(assetPath, priority, ct),
                asset => UnityEngine.Object.Instantiate(asset, position, rotation, parent), ct);

        public SceneHandle LoadSceneHandle(string scenePath, AssetLoadPriority priority = AssetLoadPriority.Normal, CancellationToken ct = default)
        {
            var sceneKey = AssetPath.ResolveScene(scenePath);
            var manager = _manager;

            return new SceneHandle(callback =>
            {
                if (manager == null)
                {
                    LiteUtils.SafeInvoke(callback, AssetLoadResult<IAssetLease<Scene?>>.Failure(
                        new AssetError(sceneKey, "Asset manager is unavailable")));
                    return;
                }

                manager.LoadSceneAsync(sceneKey, priority, callback);
            }, () => manager?.GetLoadProgress(sceneKey, typeof(Scene)) ?? 0f, ct);
        }

        /// <summary>
        /// 清理未使用的资源，包括仍在保留期内的零引用 Bundle。
        /// </summary>
        /// <param name="maxDepth">最多执行的 Bundle 缓存清理轮数。</param>
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
            return _manager?.GetVisitorInfo() ?? new VisitorInfo(null, Array.Empty<BundleVisitorInfo>());
        }
#endif
    }
}
