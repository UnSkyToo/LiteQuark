using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace LiteQuark.Runtime
{
    public abstract class BaseGameObjectPool : IGameObjectPool
    {
        public string Key { get; private set; }
        public virtual string Name => Key;
        public int CountAll => CountActive + CountInactive;
        public int CountActive => _countActive;
        public int CountInactive => Pool?.CountInactive ?? 0;
        public bool IsReady => !_isDisposed && Template != null;

        protected Transform Parent;
        protected GameObject Template;
        protected ObjectPool<GameObject> Pool;
        private readonly UniTaskCompletionSource<bool> _readyCompletion = new();
        private readonly List<InstantiateGameObjectTask> _generationTasks = new();
        private bool _isDisposed;
        // Unity's pool only counts objects created by its createFunc, not our async batches.
        private int _countActive;

        protected BaseGameObjectPool()
        {
        }

        public virtual void Initialize(string key, params object[] args)
        {
            Key = key;
            Parent = new GameObject(Name).transform;
            Parent.hideFlags = HideFlags.NotEditable;
            var root = args.Length > 0 && args[0] is Transform ? (Transform)args[0] : null;
            if (root != null)
            {
                Parent.SetParent(root, false);
            }
            Parent.localPosition = Vector3.zero;

            Pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
        }

        public virtual void Dispose()
        {
            _isDisposed = true;
            _readyCompletion.TrySetCanceled();

            while (_generationTasks.Count > 0)
            {
                var index = _generationTasks.Count - 1;
                var task = _generationTasks[index];
                _generationTasks.RemoveAt(index);
                // Cancel synchronously stops native instantiation while the template is still owned.
                task.Cancel();
            }

            Pool?.Dispose();
            _countActive = 0;

            if (Parent != null)
            {
                Object.Destroy(Parent.gameObject);
                Parent = null;
            }
        }

        protected virtual GameObject OnCreate()
        {
            if (Template == null)
            {
                return null;
            }

            var go = Object.Instantiate(Template, Parent);
            return go;
        }

        protected virtual void OnGet(GameObject go)
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        protected virtual void OnRelease(GameObject go)
        {
            go.transform.SetParent(Parent, false);
        }

        protected virtual void OnDestroy(GameObject go)
        {
            Object.Destroy(go);
        }

        public async UniTask<bool> GenerateAsync(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (!await WaitReadyAsync())
            {
                return false;
            }
            if (_isDisposed)
            {
                throw new OperationCanceledException();
            }
            if (count == 0)
            {
                return true;
            }

            var task = LiteRuntime.Task.AddInstantiateGoTask(Template, Parent, count, null);
            _generationTasks.Add(task);
            GameObject[] objects = null;
            var index = 0;
            try
            {
                // Task's await returns null on failure/cancellation; it does not throw for either state.
                objects = (GameObject[])await task.Task;
                if (_isDisposed || task.State == TaskState.Cancelled)
                {
                    throw new OperationCanceledException();
                }
                if (task.State == TaskState.Failed)
                {
                    LLog.Exception(task.Error, "Generate pool objects failed: {0}", Key);
                    return false;
                }

                for (; index < objects.Length; index++)
                {
                    // Generated objects enter the inactive pool directly; they were never borrowed.
                    Pool.Release(objects[index]);
                }
                return true;
            }
            finally
            {
                // A successful Task transfers the whole batch, including any objects not put in the pool.
                if (objects != null)
                {
                    for (; index < objects.Length; index++)
                    {
                        if (objects[index] != null)
                        {
                            Object.Destroy(objects[index]);
                        }
                    }
                }
                _generationTasks.Remove(task);
            }
        }

        public virtual void Alloc(System.Action<GameObject> callback)
        {
            Alloc(null, callback);
        }

        public virtual void Alloc(Transform parent, System.Action<GameObject> callback)
        {
            AllocWithCallbackAsync(parent, callback).Forget();
        }

        private async UniTaskVoid AllocWithCallbackAsync(Transform parent, Action<GameObject> callback)
        {
            GameObject go = null;
            try
            {
                go = await Alloc(parent);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LLog.Exception(ex, "Allocate pool object failed: {0}", Key);
            }
            LiteUtils.SafeInvoke(callback, go);
        }

        public virtual void Recycle(GameObject value)
        {
            if (value == null)
            {
                return;
            }
            Pool.Release(value);
            _countActive--;
        }

        public virtual UniTask<GameObject> Alloc()
        {
            return Alloc(parent: null);
        }

        public virtual async UniTask<GameObject> Alloc(Transform parent)
        {
            if (!await WaitReadyAsync())
            {
                return null;
            }
            if (_isDisposed)
            {
                throw new OperationCanceledException();
            }
            var go = Pool.Get();
            if (go != null && parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            if (go != null)
            {
                _countActive++;
            }
            return go;
        }

        protected void OnLoadTemplate(GameObject template)
        {
            if (_isDisposed)
            {
                return;
            }
            Template = template;
            _readyCompletion.TrySetResult(template != null);
        }

        public UniTask<bool> WaitReadyAsync()
        {
            return _isDisposed ? UniTask.FromCanceled<bool>() : _readyCompletion.Task;
        }
    }
}
