using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public class AssetHandle<T> : IAssetHandle<T> where T : UnityEngine.Object
    {
        private readonly Action<T> _releaseAction;
        private readonly UniTaskCompletionSource<T> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private T _result;
        private bool _isDisposed;
        
        public bool IsDone { get; private set; }
        public T Result => _result;
        public UniTask<T> Task => _tcs.Task;
        public UniTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();
        
        internal AssetHandle(Action<Action<T>> invoke, CancellationToken ct, Action<T> releaseAction)
        {
            _releaseAction = releaseAction;
            
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
        
        private void OnCompleted(T result)
        {
            _ctr.Dispose();
            _result = result;
            IsDone = true;

            if (!_tcs.TrySetResult(result))
            {
                if (_result != null)
                {
                    _releaseAction?.Invoke(_result);
                    _result = default;
                }
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
            
            if (!IsDone)
            {
                _tcs.TrySetCanceled();
                return;
            }
            
            if (_result != null)
            {
                _releaseAction?.Invoke(_result);
                _result = default;
            }
        }
    }
}