using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class NetworkSystem : ISystem, ISystemSettingProvider<NetworkSystemSetting>
    {
        public NetworkSystemSetting Setting { get; set; }
        public HttpClient Http { get; private set; }

        private int _activeRequests = 0;
        private readonly CancellationTokenSource _shutdownCts = new();

        public NetworkSystem()
        {
        }

        public UniTask<bool> Initialize(CancellationToken ct = default)
        {
            Http = new HttpClient(this);
            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            _shutdownCts.Cancel();
            Http?.ClearInterceptors();
            Http = null;
            return UniTask.CompletedTask;
        }

        public int MaxConcurrentRequests => Setting.MaxConcurrentRequests;
        public int DefaultTimeout => Setting.DefaultTimeout;
        public bool EnableAutoRetry => Setting.EnableAutoRetry;
        public int DefaultRetryCount => Setting.DefaultRetryCount;

        public int ActiveRequests => _activeRequests;
        internal bool IsClosing => _shutdownCts.IsCancellationRequested;
        internal CancellationToken ShutdownToken => _shutdownCts.Token;

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
