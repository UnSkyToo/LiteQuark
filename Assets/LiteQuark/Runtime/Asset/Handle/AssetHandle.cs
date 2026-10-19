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
        private AssetInfoCache.Lease<T> _lease;
        private bool _isDisposed;

        public bool IsDone { get; private set; }
        public T Result => _lease?.Asset;
        internal UniTask<T> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();

        internal AssetHandle(Action<Action<AssetInfoCache.Lease<T>>> invoke, CancellationToken ct)
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

        private void OnCompleted(AssetInfoCache.Lease<T> lease)
        {
            _ctr.Dispose();
            _lease = lease;
            IsDone = true;

            if (!_tcs.TrySetResult(_lease?.Asset))
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
