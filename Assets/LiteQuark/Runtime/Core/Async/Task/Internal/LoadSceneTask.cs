using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSceneTask : BaseTask, IResourceTaskCleanup
    {
        public override string DebugName => $"LoadScene:{_scenePath}";

        private readonly string _scenePath;
        private readonly LoadSceneParameters _parameters;
        private Action<ITask> _callback;
        private AsyncOperation _sceneRequest;
        private Scene _scene;
        private readonly UniTaskCompletionSource _cleanupCompletion = new();

        public UniTask WaitForCleanupAsync() => _cleanupCompletion.Task;

        public LoadSceneTask(string scenePath, LoadSceneParameters parameters, Action<ITask> callback)
            : base()
        {
            _scenePath = scenePath;
            _parameters = parameters;
            _callback = callback;
            _sceneRequest = null;
        }

        protected override void OnDispose()
        {
            _callback = null;
            if (_sceneRequest == null)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        public Scene GetScene()
        {
            return _scene;
        }

        protected override void OnTerminated()
        {
            if (State != TaskState.Succeeded && (_sceneRequest == null || _sceneRequest.isDone))
            {
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback, this);
            }
            if (_sceneRequest == null && State != TaskState.Succeeded)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _cleanupCompletion.TrySetResult();
            }
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _sceneRequest?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            _sceneRequest = SceneManager.LoadSceneAsync(_scenePath, _parameters);

            if (_sceneRequest == null)
            {
                Fail(new InvalidOperationException($"Could not start scene load: {_scenePath}"));
                return;
            }
            _sceneRequest.completed += OnSceneRequestLoadCompleted;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_scene.IsValid() || !scene.IsValid())
            {
                return;
            }

            if (string.Equals(scene.path, _scenePath, StringComparison.OrdinalIgnoreCase))
            {
                _scene = scene;
            }
        }

        private void OnSceneRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnSceneRequestLoadCompleted;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (State is TaskState.Failed or TaskState.Cancelled)
            {
                CleanupLateSceneAsync().Forget();
                return;
            }

            try
            {
                if (!_scene.IsValid() || !_scene.isLoaded)
                {
                    Fail(new InvalidOperationException($"Scene load returned no valid scene: {_scenePath}"));
                    return;
                }

                var callback = _callback;
                _callback = null;
                if (Complete(_scene))
                {
                    LiteUtils.SafeInvoke(callback, this);
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
            finally
            {
                _sceneRequest = null;
                _cleanupCompletion.TrySetResult();
            }
        }

        private async UniTask CleanupLateSceneAsync()
        {
            Exception failure = null;
            try
            {
                if (_scene.IsValid() && _scene.isLoaded)
                {
                    var operation = SceneManager.UnloadSceneAsync(_scene);
                    if (operation == null)
                    {
                        throw new InvalidOperationException($"Could not unload cancelled scene: {_scenePath}");
                    }

                    await UniTask.WaitUntil(() => operation.isDone);
                    if (_scene.IsValid() && _scene.isLoaded)
                    {
                        throw new InvalidOperationException($"Cancelled scene is still loaded: {_scenePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                _sceneRequest = null;
                if (failure == null)
                {
                    _cleanupCompletion.TrySetResult();
                }
                else
                {
                    _cleanupCompletion.TrySetException(failure);
                }
            }
        }
    }
}
