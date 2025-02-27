using LiteQuark.Runtime;

namespace LiteBattle.Editor
{
    public static class LiteStateUtils
    {
        private static string GetConfigFullPath(string subPath)
        {
            return PathUtils.ConcatPath(LiteStateConfig.Instance.DataPath, subPath);
        }

        public static string GetAgentRootPath()
        {
            return GetConfigFullPath("Agent");
        }

        public static string GetTimelineRootPath()
        {
            return GetConfigFullPath("Timeline");
        }
    }
}