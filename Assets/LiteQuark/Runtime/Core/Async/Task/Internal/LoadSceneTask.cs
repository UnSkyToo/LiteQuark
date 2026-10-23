using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSceneTask : BaseTask
    {
        public override string DebugName => $"LoadScene:{_scenePath}";

        private readonly string _scenePath;
        private readonly LoadSceneParameters _parameters;
        private Action<bool, Scene> _callback;
        private AsyncOperation _sceneRequest;
        private Scene _scene;

        public LoadSceneTask(string scenePath, LoadSceneParameters parameters, Action<bool, Scene> callback)
            : base()
        {
            _scenePath = scenePath;
            _parameters = parameters;
            _callback = callback;
            _sceneRequest = null;
        }

        protected override void OnDispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _callback = null;
            _sceneRequest = null;
        }

        public Scene GetScene()
        {
            return _scene;
        }

        protected override void OnTerminated()
        {
            if (State != TaskState.Succeeded && (_sceneRequest == null || _sceneRequest.isDone))
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                NotifyCompleted(false, default);
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

            if (IsDone)
            {
                return;
            }

            try
            {
                if (!_scene.IsValid() || !_scene.isLoaded)
                {
                    Fail(new InvalidOperationException($"Scene load returned no valid scene: {_scenePath}"));
                    return;
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }

            if (Complete(_scene))
            {
                NotifyCompleted(true, _scene);
            }
        }

        private void NotifyCompleted(bool success, Scene scene)
        {
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, success, scene);
        }
    }
}
