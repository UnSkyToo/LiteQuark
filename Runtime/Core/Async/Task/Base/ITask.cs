using System.Collections;

namespace LiteQuark.Runtime
{
    public interface ITask : ITick, IDispose, IEnumerator
    {
        public float Progress { get; }
        public bool IsDone { get; }
        public TaskState State { get; }
        public System.Threading.Tasks.Task Task { get; }
        
        public void Execute();
        public void Cancel();
    }
}