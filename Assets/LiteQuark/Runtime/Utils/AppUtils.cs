using System;
using System.Net.NetworkInformation;
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