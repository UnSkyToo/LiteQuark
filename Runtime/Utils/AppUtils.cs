using System;
using System.Net.NetworkInformation;
using System.Text;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class AppUtils
    {
        public static string GetCurrentPlatformName()
        {
#if UNITY_EDITOR
            return ConvertBuildTargetToString(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
#else
            return ConvertRuntimePlatformToString(Application.platform);
#endif
        }
        
#if UNITY_EDITOR
        private static string ConvertBuildTargetToString(UnityEditor.BuildTarget target)
        {
            // 将BuildTarget转换为统一字符串
            switch (target)
            {
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    return "Windows";
                case UnityEditor.BuildTarget.StandaloneOSX:
                    return "MacOS";
                case UnityEditor.BuildTarget.Android:
                    return "Android";
                case UnityEditor.BuildTarget.iOS:
                    return "iOS";
                case UnityEditor.BuildTarget.WebGL:
                    return "WebGL";
                default:
                    return target.ToString();
            }
        }
#endif

        private static string ConvertRuntimePlatformToString(RuntimePlatform platform)
        {
            // 将RuntimePlatform转换为统一字符串
            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                    return "MacOS";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                default:
                    return platform.ToString();
            }
        }

        /// <summary>
        /// Get Local App Version
        /// </summary>
        public static string GetVersion()
        {
            return Application.version;
        }

        /// <summary>
        /// Get Local Res Version
        /// </summary>
        public static string GetResVersion()
        {
            return LiteRuntime.Asset?.GetVersion() ?? GetVersion();
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

        /// <summary>
        /// 取版本号前两位
        /// </summary>
        public static string GetMainVersion()
        {
            var version = Application.version;
            var chunk = version.Split('.');
            var mainVer = new StringBuilder();
            mainVer.Append(chunk.Length > 0 ? chunk[0] : "1");
            mainVer.Append(".");
            mainVer.Append(chunk.Length > 1 ? chunk[1] : "0");
            return mainVer.ToString();
        }

        public static string GetVersionFileName()
        {
            return $"version_{GetMainVersion()}.txt";
        }
        
        public static string GetDeviceID()
        {
            return $"{SystemInfo.deviceUniqueIdentifier}{GetMacAddress()}";
        }
        
        private static string GetMacAddress()
        {
            foreach (var nf in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nf.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (nf.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
                {
                    var macAddress = nf.GetPhysicalAddress().ToString();
                    if (macAddress.Length > 0)
                    {
                        return macAddress.ToLower();
                    }
                }
            }
            
            return string.Empty;
        }
    }
}