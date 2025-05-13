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

        public override void Dispose()
        {
            if (_task != null)
            {
                _task.Cancel();
                _task.WaitForCompletion();
                _task = null;
            }
            _callback = null;
        }

        protected override void OnExecute()
        {
            _task = UnityEngine.Object.InstantiateAsync(_template, _count, _parent, Vector3.zero, Quaternion.identity);
            _task.completed += OnInstantiateComplete;
        }

        private void OnInstantiateComplete(AsyncOperation operation)
        {
            _task = null;
            
            if (operation.isDone && operation is AsyncInstantiateOperation { Result: not null } asyncOperation)
            {
                var result = new GameObject[asyncOperation.Result.Length];
                for (var i = 0; i < asyncOperation.Result.Length; ++i)
                {
                    result[i] = asyncOperation.Result[i] as GameObject;
                }
                
                _callback?.Invoke(result);
                Complete(result);
            }
            else
            {
                _callback?.Invoke(Array.Empty<GameObject>());
                Abort();
            }
        }
    }
}