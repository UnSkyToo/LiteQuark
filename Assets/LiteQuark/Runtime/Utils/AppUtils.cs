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
                var chunk1 = version1.Split('.');
                var chunk2 = version2.Split('.');
                
                for (var i = 0; i < chunk1.Length; i++)
                {
                    var v1 = int.Parse(chunk1[i]);
                    var v2 = int.Parse(chunk2[i]);
                    if (v1 > v2)
                    {
                        return 1;
                    }
                    else if (v1 < v2)
                    {
                        return -1;
                    }
                }
                
                return 0;
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