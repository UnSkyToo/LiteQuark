using System;

namespace LiteQuark.Runtime
{
    internal sealed partial class SceneInfoCache : IDispose
    {
#if UNITY_EDITOR
        internal void LoadSceneForEditor(UnityEngine.SceneManagement.Scene scene, Action<AssetLoadResult<Lease>> callback)
        {
            if (Stage != AssetCacheStage.Idle)
            {
                return;
            }

            _sceneLoaderCallback = callback;
            Stage = AssetCacheStage.Loading;

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Scene, _sceneKey, _assetType, _bundleLease?.BundlePath ?? string.Empty);
            CompleteSceneLoad(true, scene, null);
        }
#endif

        public void LoadSceneAsync(Action<AssetLoadResult<Lease>> callback)
        {
            if (Stage != AssetCacheStage.Idle)
            {
                return;
            }

            _sceneLoaderCallback = callback;
            Stage = AssetCacheStage.Loading;

            AssetLoadEventDispatcher.DispatchBegin(AssetLoadEventType.Scene, _sceneKey, _assetType, _bundleLease.BundlePath);

            try
            {
                _loadSceneTask = LiteRuntime.Task.AddLoadSceneTask(_loadPath, _parameters, HandleSceneLoadCompleted);
            }
            catch (Exception ex)
            {
                CompleteSceneLoad(false, default, new AssetError(_sceneKey, ex.Message, ex));
                throw;
            }
        }

        private void HandleSceneLoadCompleted(ITask task)
        {
            var scene = task.Result is UnityEngine.SceneManagement.Scene result ? result : default;
            var error = task.State == TaskState.Succeeded ? null : new AssetError(_sceneKey, task.Error?.Message ?? "Scene load was cancelled", task.Error);
            CompleteSceneLoad(task.State == TaskState.Succeeded, scene, error);
        }

        private void CompleteSceneLoad(bool success, UnityEngine.SceneManagement.Scene scene, AssetError error)
        {
            if (Stage == AssetCacheStage.Unloaded)
            {
                return;
            }

            _loadSceneTask = null;

            var bundlePath = _bundleLease?.BundlePath ?? string.Empty;
            var isLoaded = OnSceneLoaded(success, scene);
            if (!isLoaded)
            {
                error ??= new AssetError(_sceneKey, "Scene load returned no valid scene");
            }

            AssetLoadEventDispatcher.DispatchEnd(AssetLoadEventType.Scene, _sceneKey, _assetType, bundlePath, isLoaded, errorMessage: isLoaded ? null : "Scene load failed");

            var result = isLoaded
                ? AssetLoadResult<Lease>.Success(AcquireLease())
                : AssetLoadResult<Lease>.Failure(error);
            NotifyCompleted(result);
        }

        private void NotifyCompleted(AssetLoadResult<Lease> result)
        {
            var callback = _sceneLoaderCallback;
            _sceneLoaderCallback = null;
            LiteUtils.SafeInvoke(callback, result);
        }
    }
}
