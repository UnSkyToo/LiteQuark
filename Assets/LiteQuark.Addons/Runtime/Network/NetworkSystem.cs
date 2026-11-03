using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class NetworkSystem : ISystem, ISystemSettingProvider<NetworkSystemSetting>
    {
        public NetworkSystemSetting Setting { get; set; }

        private int _activeRequests = 0;
        // Includes slot waiters and retry delays.
        private int _pendingOperations;
        private readonly CancellationTokenSource _shutdownCts = new();
        private HttpClient _httpClient;

        public NetworkSystem()
        {
        }

        public UniTask<bool> Initialize()
        {
            _httpClient = new HttpClient(this);
            return UniTask.FromResult(true);
        }

        public async UniTask DisposeAsync()
        {
            _shutdownCts.Cancel();
            while (_pendingOperations > 0)
            {
                await UniTask.Yield();
            }

            _httpClient?.ClearInterceptors();
            _httpClient = null;
        }

        public HttpClient Http => IsClosing ? throw new ObjectDisposedException(nameof(NetworkSystem)) : _httpClient;

        public int MaxConcurrentRequests => Setting.MaxConcurrentRequests;
        public int DefaultTimeout => Setting.DefaultTimeout;
        public bool EnableAutoRetry => Setting.EnableAutoRetry;
        public int DefaultRetryCount => Setting.DefaultRetryCount;

        public int ActiveRequests => _activeRequests;
        internal bool IsClosing => _shutdownCts.IsCancellationRequested;
        internal CancellationToken ShutdownToken => _shutdownCts.Token;

        internal void ThrowIfClosing()
        {
            if (IsClosing)
            {
                throw new ObjectDisposedException(nameof(NetworkSystem));
            }
        }

        internal void BeginOperation()
        {
            ThrowIfClosing();
            _pendingOperations++;
        }

        internal void EndOperation()
        {
            _pendingOperations--;
        }

        internal void ThrowIfCancellationRequested(CancellationToken cancellationToken = default)
        {
            ShutdownToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
        }

        internal async UniTask WaitForSlot(CancellationToken cancellationToken = default)
        {
            while (_activeRequests >= MaxConcurrentRequests)
            {
                ThrowIfCancellationRequested(cancellationToken);
                await UniTask.Yield();
            }
            ThrowIfCancellationRequested(cancellationToken);
            Interlocked.Increment(ref _activeRequests);
        }

        internal void ReleaseSlot()
        {
            Interlocked.Decrement(ref _activeRequests);
        }
    }
}
