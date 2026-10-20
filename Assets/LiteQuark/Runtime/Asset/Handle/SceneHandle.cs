using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Handle for a loaded scene. Dispose unloads the scene owned by this handle.
    /// </summary>
    public class SceneHandle : IAssetHandle<Scene?>
    {
        private readonly UniTaskCompletionSource<Scene?> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private SceneInfoCache.Lease _lease;
        private bool _isDisposed;

        public bool IsDone { get; private set; }
        public Scene? Result => _lease?.Scene;
        internal UniTask<Scene?> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();

        internal SceneHandle(Action<Action<SceneInfoCache.Lease>> invoke, CancellationToken ct)
        {
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

            invoke(OnCompleted);
        }

        private void OnCompleted(SceneInfoCache.Lease lease)
        {
            _ctr.Dispose();
            _lease = lease;
            IsDone = true;

            if (!_tcs.TrySetResult(_lease?.Scene))
            {
                if (_lease != null)
                {
                    ReleaseResult();
                }
            }
        }

        private void ReleaseResult()
        {
            _lease?.Dispose();
            _lease = null;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            _ctr.Dispose();

            if (!IsDone)
            {
                _tcs.TrySetCanceled();
                return;
            }

            if (_lease != null)
            {
                ReleaseResult();
            }
        }
    }
}
