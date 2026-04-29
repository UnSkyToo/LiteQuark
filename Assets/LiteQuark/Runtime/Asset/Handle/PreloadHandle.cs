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
        private readonly UniTaskCompletionSource<bool> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private bool _isDisposed;
        private bool _result;
        private int _loadedCount;

        public bool IsDone { get; private set; }
        public bool Result => _result;
        public UniTask<bool> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();
        public IReadOnlyList<string> FailedPaths => _failedPaths;
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

            try
            {
                foreach (var assetPath in assetPaths)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Cancel(ct);
                        return;
                    }

                    _entries.Add(new Entry(assetPath, loadFunc(assetPath, ct)));
                }
            }
            catch (Exception ex)
            {
                DisposeEntries();
                IsDone = true;
                _ctr.Dispose();
                _tcs.TrySetException(ex);
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
                if (_isDisposed)
                {
                    return;
                }

                try
                {
                    var asset = await entry.Handle.Task;
                    if (_isDisposed)
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
                        _failedPaths.Add(entry.Path);
                    }
                    else
                    {
                        _loadedCount++;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    success = false;
                    _failedPaths.Add(entry.Path);
                }
                catch (Exception ex)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    success = false;
                    _failedPaths.Add(entry.Path);
                    LLog.Exception(ex, "Preload asset failed : {0}", entry.Path);
                }
            }

            if (_isDisposed)
            {
                return;
            }

            Complete(success);
        }

        private void Complete(bool success)
        {
            IsDone = true;
            _result = success;
            _ctr.Dispose();
            _tcs.TrySetResult(success);
        }

        private void Cancel(CancellationToken ct)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            IsDone = true;
            _ctr.Dispose();
            DisposeEntries();
            _tcs.TrySetCanceled(ct);
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
            DisposeEntries();

            if (!IsDone)
            {
                IsDone = true;
                _tcs.TrySetCanceled();
            }
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
