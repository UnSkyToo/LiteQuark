#if UNITY_EDITOR
using System;
using LiteQuark.Editor;
using UnityEditor;

namespace LiteQuark.Runtime
{
    internal sealed class LoadSimulateVersionPackTask : BaseTask
    {
        private readonly ResCollector _collector;
        private Action<VersionPackInfo> _callback;

        public LoadSimulateVersionPackTask(ResCollector collector, Action<VersionPackInfo> callback)
        {
            _collector = collector;
            _callback = callback;
        }

        protected override void OnExecute()
        {
            // 模拟不生成物理文件，保持逻辑 Bundle 路径，不依赖构建后才有的 Hash。
            var pack = _collector.GetVersionPackInfo(AppUtils.GetVersion(), EditorUserBuildSettings.activeBuildTarget, false, false);
            pack.Initialize();
            var callback = _callback;
            _callback = null;
            if (Complete(pack))
            {
                LiteUtils.SafeInvoke(callback, pack);
            }
        }

        protected override void OnTerminated()
        {
            if (State == TaskState.Failed)
            {
                var callback = _callback;
                _callback = null;
                LiteUtils.SafeInvoke(callback, null);
                LiteRuntime.FrameworkError(FrameworkErrorCode.LoadVersionPack, Error.Message);
            }
        }

        protected override void OnDispose()
        {
            _callback = null;
        }
    }
}
#endif
