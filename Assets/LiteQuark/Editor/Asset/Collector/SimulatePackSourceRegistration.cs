using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    internal static class SimulatePackSourceRegistration
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Register();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 关闭 Domain Reload 时也在每次进入 Play Mode 前恢复注册。
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Register();
            }
        }

        private static void Register()
        {
            SimulatePackSource.Collect = Collect;
        }

        private static (VersionPackInfo PackInfo, IReadOnlyDictionary<string, string> AssetSourcePaths) Collect()
        {
            var collector = new ResCollector();
            // 模拟保留逻辑 Bundle 路径，不依赖构建后才有的 Hash。
            var pack = collector.GetVersionPackInfo(AppUtils.GetVersion(), EditorUserBuildSettings.activeBuildTarget, false, false);
            return (pack, collector.GetAssetSourcePathsSnapshot());
        }
    }
}
