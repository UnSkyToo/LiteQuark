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

        public AssetHandleState State { get; private set; }
        public AssetError Error { get; private set; }
        public bool IsDone => State != AssetHandleState.Pending;
        public bool IsDisposed => _isDisposed;
        public GameObject Result => _instance;
        internal UniTask<GameObject> Task => _tcs.Task;
        public UniTask.Awaiter GetAwaiter() => Task.AsUniTask().GetAwaiter();

        internal GameObjectHandle(AssetHandle<GameObject> prefabHandle,
            Func<GameObject, GameObject> instantiateFunc,
            CancellationToken ct)
        {
            _prefabHandle = prefabHandle ?? throw new ArgumentNullException(nameof(prefabHandle));
            _instantiateFunc = instantiateFunc ?? throw new ArgumentNullException(nameof(instantiateFunc));

            if (ct.IsCancellationRequested)
            {
                Cancel(ct);
                return;
            }

            if (ct.CanBeCanceled)
            {
                _ctr = ct.Register(() => Cancel(ct));
            }

            InstantiateAsync(ct).Forget();
        }

        private async UniTaskVoid InstantiateAsync(CancellationToken ct)
        {
            try
            {
                var prefab = await _prefabHandle.Task;
                if (IsDone || ct.IsCancellationRequested)
                {
                    _prefabHandle.Dispose();
                    Cancel(ct);
                    return;
                }

                if (prefab != null)
                {
                    _instance = _instantiateFunc(prefab);
                }

                var error = prefab == null ? _prefabHandle.Error : null;
                if (_instance == null && error == null)
                {
                    error = new AssetError(_prefabHandle.AssetKey, "Instantiation returned no GameObject");
                }

                if (!TrySetTerminalState(_instance != null ? AssetHandleState.Succeeded : AssetHandleState.Failed, error))
                {
                    ReleaseInstance();
                    _prefabHandle.Dispose();
                    return;
                }

                if (State == AssetHandleState.Failed)
                {
                    _prefabHandle.Dispose();
                }

                _tcs.TrySetResult(_instance);
            }
            catch (OperationCanceledException)
            {
                Cancel(ct);
            }
            catch (Exception ex)
            {
                var error = new AssetError(_prefabHandle.AssetKey, ex.Message, ex);
                if (TrySetTerminalState(AssetHandleState.Failed, error))
                {
                    _prefabHandle.Dispose();
                    _tcs.TrySetException(ex);
                }
            }
            finally
            {
                _ctr.Dispose();
            }
        }

        private bool TrySetTerminalState(AssetHandleState state, AssetError error = null)
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
            if (TrySetTerminalState(AssetHandleState.Cancelled))
            {
                _prefabHandle.Dispose();
                _tcs.TrySetCanceled(ct);
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
            Cancel();
            ReleaseInstance();
            _prefabHandle.Dispose();
        }
    }
}
