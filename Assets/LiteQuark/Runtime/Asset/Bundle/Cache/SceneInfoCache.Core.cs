using System;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDisposeAsync
    {
        public AssetCacheStage Stage { get; private set; }
        public Scene? Scene => Stage == AssetCacheStage.Loaded && _scene.IsValid() && _scene.isLoaded ? _scene : null;

        private readonly string _sceneKey;
        private readonly string _loadPath;
        private readonly LoadSceneParameters _parameters;
        private readonly Type _assetType = typeof(Scene);

        private AssetBundleCache.Lease _bundleLease;
        private Scene _scene;
        private BaseTask _loadSceneTask;
        private UnityEngine.AsyncOperation _unloadOperation;
        private bool _isRequestingUnload;
        private Action<AssetLoadResult<IAssetLease<Scene?>>> _sceneLoaderCallback;

        public SceneInfoCache(AssetBundleCache.Lease bundleLease, string sceneKey, string loadPath, LoadSceneParameters parameters)
        {
            Stage = AssetCacheStage.Idle;
            _sceneKey = sceneKey;
            _loadPath = loadPath;
            _parameters = parameters;
            _bundleLease = bundleLease;
        }

        public async UniTask DisposeAsync()
        {
            var task = _loadSceneTask;
            try
            {
                if (Stage == AssetCacheStage.Loading)
                {
                    // 迟到场景归加载任务清理；保留 Bundle Lease 直到该任务收尾。
                    Stage = AssetCacheStage.Unloading;
                }
                else if (Stage != AssetCacheStage.Unloading)
                {
                    Unload();
                }
                task?.Cancel();
            }
            finally
            {
                if (_sceneLoaderCallback != null)
                {
                    NotifyCompleted(AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(_sceneKey, "Scene cache was disposed before delivery")));
                }
            }

            if (task is IResourceTaskCleanup cleanup)
            {
                await cleanup.WaitForCleanupAsync();
            }
            _loadSceneTask = null;
            await WaitForUnloadAsync();
            CompleteUnload();
        }

        internal void Unload()
        {
            if (Stage == AssetCacheStage.Unloaded || _isRequestingUnload || _unloadOperation is { isDone: false })
            {
                return;
            }

            Stage = AssetCacheStage.Unloading;

            if (_scene.isLoaded && _scene.IsValid())
            {
                RequestUnload();
            }
            else
            {
                CompleteUnload();
            }
        }

        private async UniTask WaitForUnloadAsync()
        {
            var operation = _unloadOperation;
            if (operation != null)
            {
                await UniTask.WaitUntil(() => operation.isDone);
            }
            if (_scene.IsValid() && _scene.isLoaded)
            {
                throw new InvalidOperationException($"Scene has not been unloaded: {_sceneKey}");
            }
        }

        private void RequestUnload()
        {
            var targetScene = _scene;
            _unloadOperation = null;

            UnityEngine.AsyncOperation op;
            _isRequestingUnload = true;
            try
            {
                op = _unloadOperation = SceneManager.UnloadSceneAsync(targetScene);
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Start scene unload failed : {0}", _sceneKey);
                return;
            }
            finally
            {
                _isRequestingUnload = false;
            }

            if (op == null)
            {
                LLog.Error("Start scene unload failed : {0}", _sceneKey);
                return;
            }

            op.completed += _ =>
            {
                if (!targetScene.isLoaded || !targetScene.IsValid())
                {
                    CompleteUnload();
                }
            };
        }

        private void CompleteUnload()
        {
            if (Stage == AssetCacheStage.Unloaded || _isRequestingUnload || _unloadOperation is { isDone: false })
            {
                return;
            }

            Stage = AssetCacheStage.Unloaded;
            _unloadOperation = null;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            var lease = _bundleLease;
            _bundleLease = null;
            lease?.Dispose();
        }

        private bool OnSceneLoaded(bool success, Scene scene)
        {
            if (!success || !scene.IsValid() || !scene.isLoaded)
            {
                CompleteUnload();
                LLog.Error("Load scene failed : {0}", scene.name);
                return false;
            }

            _scene = scene;
            Stage = AssetCacheStage.Loaded;

            SceneManager.sceneUnloaded += OnSceneUnloaded;
            return true;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene == _scene)
            {
                CompleteUnload();
            }
        }
    }
}
