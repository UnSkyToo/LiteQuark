using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class LoadSceneTask : BaseTask
    {
        private readonly string _sceneName;
        private readonly UnityEngine.SceneManagement.LoadSceneParameters _parameters;
        private Action<bool> _callback;
        private AsyncOperation _sceneRequest;
        private UnityEngine.SceneManagement.Scene? _scene;

        public LoadSceneTask(string sceneName, UnityEngine.SceneManagement.LoadSceneParameters parameters, Action<bool> callback)
            : base()
        {
            _sceneName = sceneName;
            _parameters = parameters;
            _callback = callback;
            _sceneRequest = null;
        }

        public override void Dispose()
        {
            _callback = null;
            _sceneRequest = null;
        }
        
        public UnityEngine.SceneManagement.Scene? GetScene()
        {
            return _scene;
        }

        protected override void OnExecute()
        {
            _sceneRequest = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_sceneName, _parameters);
            if (_sceneRequest == null)
            {
                LiteUtils.SafeInvoke(_callback, false);
                Abort();
                return;
            }
            _sceneRequest.completed += OnSceneRequestLoadCompleted;
        }
        
        protected override void OnTick(float deltaTime)
        {
            Progress = _sceneRequest?.progress ?? 0f;
        }

        private void OnSceneRequestLoadCompleted(AsyncOperation op)
        {
            op.completed -= OnSceneRequestLoadCompleted;
            _scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(_sceneName);
            LiteUtils.SafeInvoke(_callback, true);
            Complete(_scene);
        }
    }
}