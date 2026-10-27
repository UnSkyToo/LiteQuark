using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Handle for a loaded asset reference. Dispose releases the reference acquired by this handle.
    /// </summary>
    public class AssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        private readonly UniTaskCompletionSource<T> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private IAssetLease<T> _lease;
        private bool _isDisposed;

        public AssetHandleState State { get; private set; }
        public AssetError Error { get; private set; }
        public bool IsDone => State != AssetHandleState.Pending;
        public bool IsDisposed => _isDisposed;
        public T Result => _lease?.Value;
        internal string AssetKey { get; }
        internal UniTask<T> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();

        internal AssetHandle(string assetKey, Action<Action<AssetLoadResult<IAssetLease<T>>>> invoke, CancellationToken ct)
        {
            AssetKey = assetKey;
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

        private void OnCompleted(AssetLoadResult<IAssetLease<T>> result)
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
