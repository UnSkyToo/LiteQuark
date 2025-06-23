using System;
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

        public static string GetNextVersion(string version)
        {
            try
            {
                var chunk = version.Split('.');
                return $"{chunk[0]}.{chunk[1]}.{int.Parse(chunk[2]) + 1}";
            }
            catch
            {
                return version;
            }
        }

        public static int CompareVersion(string version1, string version2)
        {
            try
            {
                var v1 = int.Parse(version1.Replace(".", string.Empty));
                var v2 = int.Parse(version2.Replace(".", string.Empty));
                return v1.CompareTo(v2);
            }
            catch
            {
                return 0;
            }
        }
        
        public static string GetMainVersion()
        {
            var version = Application.version;
            var chunk = version.Split('.');
            if (chunk.Length != 3)
            {
                return string.Empty;
            }

            var mainVer = $"{chunk[0]}.{chunk[1]}";
            return mainVer;
        }

        public static string GetVersionFileName()
        {
            return $"version_{GetMainVersion()}.txt";
        }
    }
}