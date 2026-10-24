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
        private readonly List<Entry> _entries = new();
        private readonly List<string> _failedPaths = new();
        private readonly List<AssetError> _errors = new();
        private readonly UniTaskCompletionSource<bool> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private bool _isDisposed;
        private bool _result;
        private int _loadedCount;

        public AssetHandleState State { get; private set; }
        public AssetError Error { get; private set; }
        public bool IsDone => State != AssetHandleState.Pending;
        public bool IsDisposed => _isDisposed;
        public bool Result => _result;
        internal UniTask<bool> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();
        public IReadOnlyList<string> FailedPaths => _failedPaths;
        public IReadOnlyList<AssetError> Errors => _errors;
        public int TotalCount => _entries.Count;
        public int LoadedCount => _loadedCount;
        public int FailedCount => _failedPaths.Count;

        internal PreloadHandle(IEnumerable<string> assetPaths,
            Func<string, CancellationToken, AssetHandle<T>> loadFunc,
            CancellationToken ct)
        {
            if (assetPaths == null)
            {
                throw new ArgumentNullException(nameof(assetPaths));
            }

            if (loadFunc == null)
            {
                throw new ArgumentNullException(nameof(loadFunc));
            }

            if (ct.IsCancellationRequested)
            {
                Cancel(ct);
                return;
            }

            if (ct.CanBeCanceled)
            {
                _ctr = ct.Register(() => Cancel(ct));
            }

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

                    _entries.Add(new Entry(assetPath, loadFunc(assetPath, ct)));
                    if (IsDone)
                    {
                        DisposeEntries();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                var error = new AssetError(currentPath, ex.Message, ex);
                if (TryComplete(AssetHandleState.Failed, error))
                {
                    DisposeEntries();
                    _tcs.TrySetException(ex);
                }
                return;
            }

            if (_entries.Count == 0)
            {
                Complete(true);
                return;
            }

            WaitAsync(ct).Forget();
        }

        private async UniTaskVoid WaitAsync(CancellationToken ct)
        {
            var success = true;

            foreach (var entry in _entries)
            {
                if (IsDone)
                {
                    return;
                }

                try
                {
                    var asset = await entry.Handle.Task;
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
                        success = false;
                        AddError(entry, entry.Handle.Error ?? new AssetError(entry.Path, "Asset load returned no result"));
                    }
                    else
                    {
                        _loadedCount++;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (IsDone)
                    {
                        return;
                    }

                    if (ct.IsCancellationRequested)
                    {
                        Cancel(ct);
                        return;
                    }

                    success = false;
                    AddError(entry, new AssetError(entry.Path, "Preload child request was cancelled"));
                }
                catch (Exception ex)
                {
                    if (IsDone)
                    {
                        return;
                    }

                    success = false;
                    AddError(entry, entry.Handle.Error ?? new AssetError(entry.Path, ex.Message, ex));
                    LLog.Exception(ex, "Preload asset failed : {0}", entry.Path);
                }
            }

            if (IsDone)
            {
                return;
            }

            Complete(success);
        }

        private void Complete(bool success)
        {
            if (!TryComplete(success ? AssetHandleState.Succeeded : AssetHandleState.Failed,
                success ? null : _errors[0]))
            {
                return;
            }

            _result = success;
            _tcs.TrySetResult(success);
        }

        private void AddError(Entry entry, AssetError error)
        {
            _failedPaths.Add(entry.Path);
            _errors.Add(error);
        }

        private bool TryComplete(AssetHandleState state, AssetError error = null)
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
            if (TryComplete(AssetHandleState.Cancelled))
            {
                DisposeEntries();
                _tcs.TrySetCanceled(ct);
            }
        }

        private void DisposeEntries()
        {
            foreach (var entry in _entries)
            {
                entry.Handle?.Dispose();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _ctr.Dispose();
            Cancel();
            DisposeEntries();
        }

        private readonly struct Entry
        {
            public readonly string Path;
            public readonly AssetHandle<T> Handle;

            public Entry(string path, AssetHandle<T> handle)
            {
                Path = path;
                Handle = handle;
            }
        }
    }
}
