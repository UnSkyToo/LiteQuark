using System;
using System.Collections;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface ITask : IEnumerator
    {
        public float Progress { get; }
        public bool IsExecuted { get; }
        public bool IsDone { get; }
        public object Result { get; }
        public Exception Error { get; }
        public TaskState State { get; }
        public UniTask<object> Task { get; }

        public void Cancel();
    }
}
