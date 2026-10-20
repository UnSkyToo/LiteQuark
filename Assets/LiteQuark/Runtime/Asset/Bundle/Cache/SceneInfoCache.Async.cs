using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDispose
    {
#if UNITY_EDITOR
        internal void LoadSceneForEditor(UnityEngine.SceneManagement.Scene scene, Action<Lease> callback)
        {
            if (Stage != AssetCacheStage.Created)
            {
                return;
            }

            _sceneLoaderCallback = callback;
            Stage = AssetCacheStage.Loading;

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Scene, _sceneName, _bundleLease?.BundlePath ?? string.Empty);
            HandleSceneLoadCompleted(true, scene);
        }
#endif

        public void LoadSceneAsync(Action<Lease> callback)
        {
            if (Stage != AssetCacheStage.Created)
            {
                return;
            }

            _sceneLoaderCallback = callback;
            Stage = AssetCacheStage.Loading;

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Scene, _sceneName, _bundleLease.BundlePath);

            try
            {
                _loadSceneTask = LiteRuntime.Task.AddLoadSceneTask(_sceneName, _parameters, HandleSceneLoadCompleted);
            }
            catch
            {
                HandleSceneLoadCompleted(false, default);
                throw;
            }
        }

        private void HandleSceneLoadCompleted(bool success, UnityEngine.SceneManagement.Scene scene)
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }

            _loadSceneTask = null;

            var isLoaded = OnSceneLoaded(success, scene);

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Scene, _sceneName, _bundleLease?.BundlePath ?? string.Empty, isLoaded, errorMessage: isLoaded ? null : "Scene load failed");

            var lease = isLoaded ? AcquireLease() : null;
            NotifyCompleted(lease);
        }

        private void NotifyCompleted(Lease lease)
        {
            var callback = _sceneLoaderCallback;
            _sceneLoaderCallback = null;
            LiteUtils.SafeInvoke(callback, lease);
        }
    }
}
