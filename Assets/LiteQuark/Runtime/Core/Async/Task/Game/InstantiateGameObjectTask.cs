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
                DestroyResults(task.Result);
            }
        }

        private static void DestroyResults(UnityEngine.Object[] objects)
        {
            if (objects == null)
            {
                return;
            }
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
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

            if (_task == null)
            {
                return;
            }

            GameObject[] result;
            try
            {
                var nativeResult = _task.Result;
                if (!operation.isDone || nativeResult == null)
                {
                    Fail(new InvalidOperationException("Instantiation returned no result."));
                    return;
                }

                // Unity 2022.3 reinterprets Object[] as T[]; publish a real GameObject[] through Task.Result.
                result = new GameObject[nativeResult.Length];
                for (var i = 0; i < nativeResult.Length; ++i)
                {
                    result[i] = nativeResult[i] as GameObject;
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }

            // PublishResult may resume callers and retire the wrapper before the callback runs.
            var callback = _callback;
            _callback = null;
            _task = null;
            if (Complete(result))
            {
                LiteUtils.SafeInvoke(callback, result);
            }
            else
            {
                DestroyResults(result);
            }
        }
    }
}
