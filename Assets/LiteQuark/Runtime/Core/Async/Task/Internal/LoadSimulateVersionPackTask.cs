#if UNITY_EDITOR
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSimulateVersionPackTask : BaseTask
    {
        internal IReadOnlyDictionary<string, string> AssetSourcePaths { get; private set; }

        protected override void OnExecute()
        {
            var (pack, sourcePaths) = SimulatePackSource.CreateSnapshot();
            pack.Initialize();
            // Complete 会恢复初始化等待者，必须先发布同一会话的路径快照。
            AssetSourcePaths = sourcePaths;
            Complete(pack);
        }

        protected override void OnTerminated()
        {
            if (State == TaskState.Failed)
            {
                LiteRuntime.FrameworkError(FrameworkErrorCode.LoadVersionPack, Error.Message);
            }
        }
    }
}
#endif
