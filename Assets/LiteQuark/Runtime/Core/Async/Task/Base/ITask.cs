using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>任务的观察、等待与取消接口；提交和实现任务使用 BaseTask。</summary>
    public interface ITask
    {
        public float Progress { get; }
        public bool IsExecuted { get; }
        public bool IsDone { get; }
        public object Result { get; }
        public Exception Error { get; }
        public TaskState State { get; }

        /// <summary>等待任务进入任一终态。等待结束后通过 State、Result 和 Error 获取结果。</summary>
        public UniTask WaitForCompletionAsync();
        public void Cancel();
    }
}
