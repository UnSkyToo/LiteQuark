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
        private IAssetLease<Scene?> _lease;
        private bool _isDisposed;

        public AssetHandleState State { get; private set; }
        public AssetError Error { get; private set; }
        public bool IsDone => State != AssetHandleState.Pending;
        public bool IsDisposed => _isDisposed;
        public Scene? Result => _lease?.Value;
        internal UniTask<Scene?> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();

        internal SceneHandle(Action<Action<AssetLoadResult<IAssetLease<Scene?>>>> invoke, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                Cancel(ct);
                return;
            }

            if (ct.CanBeCanceled)
            {
                _ctr = ct.Register(() => Cancel(ct));
            }

            invoke(OnCompleted);
        }

        private void OnCompleted(AssetLoadResult<IAssetLease<Scene?>> result)
        {
            if (!TrySetTerminalState(result.Succeeded ? AssetHandleState.Succeeded : AssetHandleState.Failed, result.Error))
            {
                result.Value?.Dispose();
                return;
            }

            _lease = result.Value;
            _tcs.TrySetResult(_lease?.Value);
        }

        private bool TrySetTerminalState(AssetHandleState state, AssetError error = null)
        {
            if (IsDone)
            {
                return false;
            }

            State = state;
            Error = error;
            _ctr.Dispose();
            return true;
        }

        private void Cancel(CancellationToken ct = default)
        {
            if (TrySetTerminalState(AssetHandleState.Cancelled))
            {
                _tcs.TrySetCanceled(ct);
            }
        }

        private void ReleaseLease()
        {
            var lease = _lease;
            _lease = null;
            lease?.Dispose();
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
                Cancel();
                return;
            }

            if (_lease != null)
            {
                ReleaseLease();
            }
        }
    }
}
