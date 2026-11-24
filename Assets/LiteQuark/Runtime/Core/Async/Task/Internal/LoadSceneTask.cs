using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal class LoadSceneTask : BaseTask
    {
        public override string DebugName => $"LoadScene:{_scenePath}";

        private readonly string _scenePath;
        private Action<LoadSceneTask> _callback;
        private AsyncOperation _sceneRequest;
        private Scene _scene;

        public LoadSceneTask(string scenePath, Action<LoadSceneTask> callback)
            : base()
        {
            _scenePath = scenePath;
            _callback = callback;
            _sceneRequest = null;
        }

        public Scene GetScene()
        {
            return _scene;
        }

        protected override void OnTerminated()
        {
            if (_sceneRequest == null && State != TaskState.Succeeded)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                NotifyCompleted();
            }
        }

        private void NotifyCompleted()
        {
            var callback = _callback;
            _callback = null;
            LiteUtils.SafeInvoke(callback, this);
        }

        protected override void OnTick(float deltaTime)
        {
            Progress = _sceneRequest?.progress ?? 0f;
        }

        protected override void OnExecute()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            _sceneRequest = StartLoadScene(_scenePath);

            if (_sceneRequest == null)
            {
                Fail(new InvalidOperationException($"Could not start scene load: {_scenePath}"));
                return;
            }
            _sceneRequest.completed += OnSceneRequestLoadCompleted;
        }

        protected virtual AsyncOperation StartLoadScene(string path)
        {
            return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
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

            _sceneRequest = null;
            if (!IsDone)
            {
                if (_scene.IsValid() && _scene.isLoaded)
                {
                    Complete(_scene);
                }
                else
                {
                    Fail(new InvalidOperationException($"Scene load returned no valid scene: {_scenePath}"));
                }
            }
            NotifyCompleted();
        }
    }
}
