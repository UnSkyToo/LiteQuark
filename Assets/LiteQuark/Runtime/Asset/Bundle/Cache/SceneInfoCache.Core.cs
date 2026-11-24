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
        private readonly Type _assetType = typeof(Scene);

        private readonly AssetBundleCache.Lease _bundleLease;
        private Scene _scene;
        private BaseTask _loadSceneTask;
        private UnityEngine.AsyncOperation _unloadOperation;
        private bool _isRequestingUnload;
        private Action<AssetLoadResult<IAssetLease<Scene?>>> _sceneLoaderCallback;

        public SceneInfoCache(AssetBundleCache.Lease bundleLease, string sceneKey)
        {
            Stage = AssetCacheStage.Idle;
            _sceneKey = sceneKey;
            _bundleLease = bundleLease ?? throw new ArgumentNullException(nameof(bundleLease));
        }

        public async UniTask DisposeAsync()
        {
            _loadSceneTask?.Cancel();
            if (_sceneLoaderCallback != null)
            {
                NotifyCompleted(AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(_sceneKey, "Scene cache was disposed before delivery")));
            }
            while (_loadSceneTask != null || _isRequestingUnload || _unloadOperation is { isDone: false })
            {
                await UniTask.Yield();
            }
            // 已交付的场景仍由 Handle 卸载，关闭只归还资源引用。
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _bundleLease.Dispose();
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

        internal void CompleteUnload()
        {
            if (Stage == AssetCacheStage.Unloaded || _isRequestingUnload || _unloadOperation is { isDone: false })
            {
                return;
            }

            Stage = AssetCacheStage.Unloaded;
            _unloadOperation = null;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            _bundleLease.Dispose();
        }

        private bool OnSceneLoaded(bool success, Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                CompleteUnload();
                return false;
            }

            _scene = scene;
            Stage = AssetCacheStage.Loaded;

            SceneManager.sceneUnloaded += OnSceneUnloaded;
            if (!success)
            {
                Unload();
            }
            return success;
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
