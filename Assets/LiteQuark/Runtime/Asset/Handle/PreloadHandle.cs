using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Handle for preloading and holding a batch of asset references.
    /// </summary>
    public class PreloadHandle<T> : IAssetHandle<bool> where T : UnityEngine.Object
    {
        private readonly List<AssetHandle<T>> _handles = new();
        private readonly List<string> _failedPaths = new();
        private readonly List<AssetError> _errors = new();
        private readonly UniTaskCompletionSource<bool> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private AssetLoadProgress _progress;

        public AssetHandleState State { get; private set; }
        public AssetError Error { get; private set; }
        public bool IsDone => State != AssetHandleState.Pending;
        public bool IsDisposed { get; private set; }
        public float Progress => _progress.Read();
        public bool Result => State == AssetHandleState.Succeeded;
        internal UniTask<bool> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();
        public IReadOnlyList<string> FailedPaths => _failedPaths;
        public IReadOnlyList<AssetError> Errors => _errors;
        public int TotalCount { get; private set; }
        public int LoadedCount { get; private set; }
        public int FailedCount => _failedPaths.Count;

        internal PreloadHandle(IEnumerable<string> assetPaths,
            Func<string, AssetHandle<T>> loadFunc,
            CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                Cancel(ct);
                return;
            }

            if (ct.CanBeCanceled)
            {
                // 只有批次订阅 token，取消时统一释放子 Handle。
                _ctr = ct.Register(() => Cancel(ct));
            }

            _progress = new AssetLoadProgress(ReadBatchProgress);
            string currentPath = null;
            try
            {
                foreach (var assetPath in assetPaths)
                {
                    currentPath = assetPath;
                    if (ct.IsCancellationRequested)
                    {
                        Cancel(ct);
                        return;
                    }

                    _handles.Add(loadFunc(assetPath));
                    TotalCount++;
                    if (IsDone)
                    {
                        // 提交子请求期间可能同步取消，刚返回的 Handle 也由本批次释放。
                        ReleaseHandles();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                var error = new AssetError(currentPath, ex.Message, ex);
                if (TrySetTerminalState(AssetHandleState.Failed, error))
                {
                    ReleaseHandles();
                    _tcs.TrySetException(ex);
                }
                return;
            }

            WaitAsync(ct).Forget();
        }

        private float ReadBatchProgress()
        {
            if (_handles.Count == 0)
            {
                return 0f;
            }

            var total = 0f;
            foreach (var handle in _handles)
            {
                total += handle.Progress;
            }
            return total / _handles.Count;
        }

        private async UniTaskVoid WaitAsync(CancellationToken ct)
        {
            try
            {
                foreach (var handle in _handles)
                {
                    var asset = await handle.Task;
                    if (IsDone)
                    {
                        return;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        Cancel(ct);
                        return;
                    }

                    if (asset == null)
                    {
                        _failedPaths.Add(handle.AssetKey);
                        _errors.Add(handle.Error ?? new AssetError(handle.AssetKey, "Asset load returned no result"));
                    }
                    else
                    {
                        LoadedCount++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Cancel(ct);
                return;
            }

            var success = _errors.Count == 0;
            if (TrySetTerminalState(success ? AssetHandleState.Succeeded : AssetHandleState.Failed,
                success ? null : _errors[0]))
            {
                _tcs.TrySetResult(success);
            }
        }

        private bool TrySetTerminalState(AssetHandleState state, AssetError error = null)
        {
            if (IsDone)
            {
                return false;
            }

            State = state;
            _progress.Complete(state);
            Error = error;
            _ctr.Dispose();
            return true;
        }

        private void Cancel(CancellationToken ct = default)
        {
            if (TrySetTerminalState(AssetHandleState.Cancelled))
            {
                ReleaseHandles();
                _tcs.TrySetCanceled(ct);
            }
        }

        private void ReleaseHandles()
        {
            foreach (var handle in _handles)
            {
                handle.Dispose();
            }
            _handles.Clear();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            if (!IsDone)
            {
                Cancel();
            }
            else if (State != AssetHandleState.Cancelled)
            {
                ReleaseHandles();
            }
        }
    }
}
