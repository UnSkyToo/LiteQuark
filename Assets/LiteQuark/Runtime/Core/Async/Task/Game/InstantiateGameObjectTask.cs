using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class InstantiateGameObjectTask : BaseTask
    {
        private readonly GameObject _template;
        private readonly Transform _parent;
        private readonly int _count;

        private AsyncInstantiateOperation<GameObject> _task;
        private Action<GameObject[]> _callback;

        public InstantiateGameObjectTask(GameObject template, Transform parent, int count, Action<GameObject[]> callback)
            : base()
        {
            _template = template;
            _parent = parent;
            _count = count;

            _callback = callback;
        }

        protected override void OnDispose()
        {
            StopOperation();
            _callback = null;
        }

        protected override void OnTerminated()
        {
            try
            {
                if (State is TaskState.Failed or TaskState.Cancelled)
                {
                    StopOperation();
                }
            }
            finally
            {
                if (State == TaskState.Failed)
                {
                    var callback = _callback;
                    _callback = null;
                    LiteUtils.SafeInvoke(callback, Array.Empty<GameObject>());
                }
            }
        }

        private void StopOperation()
        {
            var task = _task;
            _task = null;
            if (task != null)
            {
                task.completed -= OnInstantiateComplete;
                task.Cancel();
                task.WaitForCompletion();
            }
        }

        protected override void OnExecute()
        {
            _task = UnityEngine.Object.InstantiateAsync(_template, _count, _parent, Vector3.zero, Quaternion.identity);
            _task.completed += OnInstantiateComplete;
        }

        private void OnInstantiateComplete(AsyncOperation operation)
        {
            operation.completed -= OnInstantiateComplete;
            _task = null;

            if (IsDone)
            {
                return;
            }

            GameObject[] result;
            try
            {
                if (!operation.isDone || operation is not AsyncInstantiateOperation { Result: not null } asyncOperation)
                {
                    Fail(new InvalidOperationException("Instantiation returned no result."));
                    return;
                }

                result = new GameObject[asyncOperation.Result.Length];
                for (var i = 0; i < asyncOperation.Result.Length; ++i)
                {
                    result[i] = asyncOperation.Result[i] as GameObject;
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }

            if (Complete(result))
            {
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback, result);
            }
        }
    }
}
