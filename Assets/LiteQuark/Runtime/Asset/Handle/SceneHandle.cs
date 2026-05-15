using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Handle for a loaded scene. Dispose unloads the scene owned by this handle.
    /// </summary>
    public class SceneHandle : IAssetHandle<bool>
    {
        private readonly Action<Scene> _releaseAction;
        private readonly UniTaskCompletionSource<bool> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private bool _isDisposed;
        private bool _isLoaded;
        private Scene _scene;

        public bool IsDone { get; private set; }
        public bool Result => _isLoaded;
        public UniTask<bool> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();

        internal SceneHandle(Action<Action<bool, Scene>> invoke, CancellationToken ct, Action<Scene> releaseAction)
        {
            _releaseAction = releaseAction;

            if (ct.IsCancellationRequested)
            {
                IsDone = true;
                _tcs.TrySetCanceled(ct);
                return;
            }

            if (ct.CanBeCanceled)
            {
                _ctr = ct.Register(() => _tcs.TrySetCanceled(ct));
            }

            invoke(OnSceneLoaded);
        }

        private void OnSceneLoaded(bool success, Scene scene)
        {
            _ctr.Dispose();
            _scene = scene;
            success = success && scene.IsValid() && scene.isLoaded;
            _isLoaded = success;
            IsDone = true;

            if (!_tcs.TrySetResult(success))
            {
                if (success)
                {
                    ReleaseScene();
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _ctr.Dispose();

            if (!IsDone)
            {
                _tcs.TrySetCanceled();
                return;
            }

            if (_isLoaded)
            {
                ReleaseScene();
            }
        }

        private void ReleaseScene()
        {
            _releaseAction?.Invoke(_scene);
            _isLoaded = false;
        }
    }
}
