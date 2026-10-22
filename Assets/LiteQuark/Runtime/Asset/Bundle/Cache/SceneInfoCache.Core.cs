using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDispose
    {
        public AssetCacheStage Stage { get; private set; }
        public UnityEngine.SceneManagement.Scene? Scene => Stage == AssetCacheStage.Loaded && _scene.IsValid() && _scene.isLoaded ? _scene : null;

        private readonly string _sceneKey;
        private readonly string _loadPath;
        private readonly UnityEngine.SceneManagement.LoadSceneParameters _parameters;
        private readonly Type _assetType = typeof(UnityEngine.SceneManagement.Scene);

        private AssetBundleCache.Lease _bundleLease;
        private UnityEngine.SceneManagement.Scene _scene;
        private BaseTask _loadSceneTask;
        private Action<Lease> _sceneLoaderCallback;

        public SceneInfoCache(AssetBundleCache.Lease bundleLease, string sceneKey, string loadPath, UnityEngine.SceneManagement.LoadSceneParameters parameters)
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
                NotifyCompleted(null);
            }
        }

        public void Unload()
        {
            if (Stage is AssetCacheStage.Unloading or AssetCacheStage.Unloaded)
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

            try
            {
                var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(targetScene);
                if (op == null)
                {
                    LLog.Error("Start scene unload failed : {0}", _sceneKey);
                    return;
                }

                LiteRuntime.Task.AddTask(op, () =>
                {
                    if (!targetScene.isLoaded || !targetScene.IsValid())
                    {
                        CompleteUnload();
                    }
                });
            }
            catch(Exception ex)
            {
                LLog.Exception(ex, "Start scene unload failed : {0}", _sceneKey);
            }
        }

        private void CompleteUnload()
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }

            Stage = AssetCacheStage.Unloaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;

            _bundleLease?.Dispose();
        }

        private bool OnSceneLoaded(bool success, UnityEngine.SceneManagement.Scene scene)
        {
            if (!success || !scene.IsValid() || !scene.isLoaded)
            {
                CompleteUnload();
                LLog.Error("Load scene failed : {0}", scene.name);
                return false;
            }

            _scene = scene;
            Stage = AssetCacheStage.Loaded;

            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            return true;
        }

        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene == _scene)
            {
                CompleteUnload();
            }
        }
    }
}
