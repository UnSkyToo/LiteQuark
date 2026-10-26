using System;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDispose
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

        public void Dispose()
        {
            try
            {
                Unload();

                _loadSceneTask?.Cancel();
                _loadSceneTask = null;
            }
            finally
            {
                if (_sceneLoaderCallback != null)
                {
                    NotifyCompleted(AssetLoadResult<IAssetLease<Scene?>>.Failure(new AssetError(_sceneKey, "Scene cache was disposed before delivery")));
                }
            }
        }

        public void Unload()
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
                op = SceneManager.UnloadSceneAsync(targetScene);
                if (Stage == AssetCacheStage.Unloaded)
                {
                    return;
                }
                _unloadOperation = op;
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

            try
            {
                LiteRuntime.Task.AddTask(op, () =>
                {
                    if (!targetScene.isLoaded || !targetScene.IsValid())
                    {
                        CompleteUnload();
                    }
                });
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Observe scene unload failed : {0}", _sceneKey);
            }
        }

        private void CompleteUnload()
        {
            if (Stage == AssetCacheStage.Unloaded)
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
