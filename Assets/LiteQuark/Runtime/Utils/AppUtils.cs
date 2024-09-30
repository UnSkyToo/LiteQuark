using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class AppUtils
    {
        public static string GetCurrentPlatform()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();
#else
            return Application.platform.ToString();
#endif
        }

        public static string GetVersion()
        {
            return Application.version;
        }
    }
}