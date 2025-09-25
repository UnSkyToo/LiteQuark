using System.Collections;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface ITask : ITick, IDispose, IEnumerator
    {
        public float Progress { get; }
        public bool IsDone { get; }
        public object Result { get; }
        public TaskState State { get; }
        public UniTask<object> Task { get; }
        
        public void Execute();
        public void Cancel();
    }
}