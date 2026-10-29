using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal static class UniTaskUtils
    {
        /// <summary>
        /// 等待所有任务结束后，抛出按输入顺序观察到的首个异常或取消。
        /// 列表不得为 null，且调用方在等待结束前不得修改列表。
        /// </summary>
        internal static async UniTask WhenAllCompleteAsync(IReadOnlyList<UniTask> tasks)
        {
            Exception failure = null;
            foreach (var task in tasks)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    failure ??= ex;
                }
            }
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
