using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Handle for an instantiated GameObject. Dispose destroys the instance and releases its prefab handle.
    /// </summary>
    public class GameObjectHandle : IAssetHandle<GameObject>
    {
        private readonly AssetHandle<GameObject> _prefabHandle;
        private readonly Func<GameObject, GameObject> _instantiateFunc;
        private readonly UniTaskCompletionSource<GameObject> _tcs = new();
        private CancellationTokenRegistration _ctr;
        private GameObject _instance;
        private bool _isDisposed;

        public bool IsDone { get; private set; }
        public GameObject Result => _instance;
        public UniTask<GameObject> Task => _tcs.Task;
        public UniTask<GameObject>.Awaiter GetAwaiter() => Task.GetAwaiter();

        internal GameObjectHandle(AssetHandle<GameObject> prefabHandle,
            Func<GameObject, GameObject> instantiateFunc,
            CancellationToken ct)
        {
            _prefabHandle = prefabHandle ?? throw new ArgumentNullException(nameof(prefabHandle));
            _instantiateFunc = instantiateFunc ?? throw new ArgumentNullException(nameof(instantiateFunc));

            if (ct.IsCancellationRequested)
            {
                IsDone = true;
                _tcs.TrySetCanceled(ct);
                _prefabHandle.Dispose();
                return;
            }

            if (ct.CanBeCanceled)
            {
                _ctr = ct.Register(() => _tcs.TrySetCanceled(ct));
            }

            InstantiateAsync(ct).Forget();
        }

        private async UniTaskVoid InstantiateAsync(CancellationToken ct)
        {
            try
            {
                var prefab = await _prefabHandle.Task;
                if (_isDisposed || ct.IsCancellationRequested)
                {
                    _prefabHandle.Dispose();
                    if (ct.IsCancellationRequested)
                    {
                        IsDone = true;
                    }
                    _tcs.TrySetCanceled(ct);
                    return;
                }

                if (prefab != null)
                {
                    _instance = _instantiateFunc(prefab);
                }

                IsDone = true;
                if (!_tcs.TrySetResult(_instance))
                {
                    ReleaseInstance();
                    _prefabHandle.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                _prefabHandle.Dispose();
                IsDone = true;
                _tcs.TrySetCanceled(ct);
            }
            catch (Exception ex)
            {
                _prefabHandle.Dispose();
                IsDone = true;
                _tcs.TrySetException(ex);
            }
            finally
            {
                _ctr.Dispose();
            }
        }

        private void ReleaseInstance()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
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
            ReleaseInstance();
            _prefabHandle.Dispose();

            if (!IsDone)
            {
                _tcs.TrySetCanceled();
            }
        }
    }
}
