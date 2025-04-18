using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class InstantiateGameObjectTask : BaseTask
    {
        private readonly GameObject Template_;
        private readonly Transform Parent_;
        private readonly int Count_;

        private AsyncInstantiateOperation<GameObject> Task_;
        private Action<GameObject[]> Callback_;

        public InstantiateGameObjectTask(GameObject template, Transform parent, int count, Action<GameObject[]> callback)
            : base()
        {
            Template_ = template;
            Parent_ = parent;
            Count_ = count;
            
            Callback_ = callback;
        }

        public override void Dispose()
        {
            if (Task_ != null)
            {
                Task_.Cancel();
                Task_.WaitForCompletion();
                Task_ = null;
            }
            Callback_ = null;
        }

        protected override void OnExecute()
        {
            Task_ = UnityEngine.Object.InstantiateAsync(Template_, Count_, Parent_, Vector3.zero, Quaternion.identity);
            Task_.completed += OnInstantiateComplete;
        }

        private void OnInstantiateComplete(AsyncOperation operation)
        {
            Task_ = null;
            
            if (operation.isDone && operation is AsyncInstantiateOperation { Result: not null } asyncOperation)
            {
                var result = new GameObject[asyncOperation.Result.Length];
                for (var i = 0; i < asyncOperation.Result.Length; ++i)
                {
                    result[i] = asyncOperation.Result[i] as GameObject;
                }
                
                Callback_?.Invoke(result);
                Complete(result);
            }
            else
            {
                Callback_?.Invoke(Array.Empty<GameObject>());
                Abort();
            }
        }
    }
}