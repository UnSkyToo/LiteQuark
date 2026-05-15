using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSceneTask : BaseTask
    {
        public override string DebugName => $"LoadScene:{_sceneName}";
        
        private readonly string _sceneName;
        private readonly LoadSceneParameters _parameters;
        private Action<bool, Scene> _callback;
        private AsyncOperation _sceneRequest;
        private Scene _scene;

        public LoadSceneTask(string sceneName, LoadSceneParameters parameters, Action<bool, Scene> callback)
            : base()
        {
            _sceneName = sceneName;
            _parameters = parameters;
            _callback = callback;
            _sceneRequest = null;
        }

        public override void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _callback = null;
            _sceneRequest = null;
        }
        
        public Scene GetScene()
        {
            return _scene;
        }
        
        protected override void OnTick(float deltaTime)
        {
            Progress = _sceneRequest?.progress ?? 0f;
        }
        
        protected override void OnExecute()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            _sceneRequest = SceneManager.LoadSceneAsync(_sceneName, _parameters);
            if (_sceneRequest == null)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Cancel();
                LiteUtils.SafeInvoke(_callback, false, default);
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

            if (string.Equals(scene.name, _sceneName, StringComparison.OrdinalIgnoreCase))
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
            
            var success = _scene.IsValid() && _scene.isLoaded;
            Complete(success ? _scene : default(Scene));
            LiteUtils.SafeInvoke(_callback, success, _scene);
        }
    }
}
