using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public sealed class NetworkSystem : ISystem, ISystemSettingProvider<NetworkSystemSetting>
    {
        public NetworkSystemSetting Setting { get; set; }
        
        private int _activeRequests = 0;
        private HttpClient _httpClient;

        public NetworkSystem()
        {
        }

        public UniTask<bool> Initialize()
        {
            _httpClient = new HttpClient(this);
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            _httpClient = null;
            _activeRequests = 0;
        }

        public HttpClient Http => _httpClient;

        public int MaxConcurrentRequests => Setting.MaxConcurrentRequests;
        public int DefaultTimeout => Setting.DefaultTimeout;
        public bool EnableAutoRetry => Setting.EnableAutoRetry;
        public int DefaultRetryCount => Setting.DefaultRetryCount;

        public int ActiveRequests => _activeRequests;

        internal async UniTask WaitForSlot()
        {
            while (_activeRequests >= MaxConcurrentRequests)
            {
                await UniTask.Yield();
            }
            Interlocked.Increment(ref _activeRequests);
        }

        internal void ReleaseSlot()
        {
            Interlocked.Decrement(ref _activeRequests);
        }
    }
}
