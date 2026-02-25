using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    public class SceneHandle : IAssetHandle<bool>
    {
        private readonly string _scenePath;
        private readonly UniTaskCompletionSource<bool> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private bool _isDisposed;
        private bool _isLoaded;

        public bool IsDone { get; private set; }
        public bool Result => _isLoaded;
        public UniTask<bool> Task => _tcs.Task;
        public UniTask<bool>.Awaiter GetAwaiter() => Task.GetAwaiter();

        internal SceneHandle(string scenePath, LoadSceneParameters parameters, CancellationToken ct)
        {
            _scenePath = scenePath;

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

            LiteRuntime.Asset.LoadSceneAsync(scenePath, parameters, OnSceneLoaded);
        }

        private void OnSceneLoaded(bool success)
        {
            _ctr.Dispose();
            _isLoaded = success;
            IsDone = true;

            if (!_tcs.TrySetResult(success))
            {
                if (success)
                {
                    LiteRuntime.Asset.UnloadSceneAsync(_scenePath);
                    _isLoaded = false;
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
                LiteRuntime.Asset.UnloadSceneAsync(_scenePath);
                _isLoaded = false;
            }
        }
    }
}