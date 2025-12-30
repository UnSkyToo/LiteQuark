using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    /// <summary>
    /// CI Build Command Line
    /// -debugBuild : is debug build (true/false)
    /// -target : build target (Android/iOS/WebGL)
    /// -enableApp : isBuildApp (true/false)
    /// -enableRes : isBuildRes (true/false)
    /// -enableCustom : isBuildCustom (true/false)
    /// -appVersion : app version (1.0.0)
    /// -appCode : app build code (1)
    /// -hashMode : enable hashMode (true/false)
    /// -flatMode : enable flatMode (true/flase)
    /// -copyToStreaming : copy to streaming assets (true/false)
    /// -arm64 : enable arm64 archive, only android (true/false)
    /// -isAAB : enable aab mode, only android (true/false)
    /// -splitApp : enable split apk, only android (true/false)
    /// -createSymbols : enable create symbol file, only android (true/false)
    /// </summary>
    public static class ProjectBuilderCommandLine
    {
        public static void Build()
        {
            var isSuccess = BuildProject();
            EditorApplication.Exit(isSuccess ? 0 : 1);
        }

        private static bool BuildProject()
        {
            var argumentData = ParseArgument();

            var target = argumentData.GetTarget();
            if (target == BuildTarget.NoTarget)
            {
                Debug.LogError("Please specify the target to build.");
                return false;
            }

            var version = argumentData.GetAppVersion();

            var resCfg = new ResBuildConfig
            {
                Enable = argumentData.GetEnableResBuild(),
                IncrementBuildModel = false,
                HashMode = argumentData.GetHashMode(),
                FlatMode = argumentData.GetFlatMode(),
                CopyToStreamingAssets = argumentData.GetCopyToStreaming(),
                CleanStreamingAssetsBeforeCopy = true,
            };

            var appCfg = new AppBuildConfig
            {
                Enable = argumentData.GetEnableAppBuild(),
                CleanBuildMode = true,
                Identifier = PlayerSettings.applicationIdentifier,
                ProduceName = PlayerSettings.productName,
                BuildCode = argumentData.GetAppCode(),
                Backend = ScriptingImplementation.IL2CPP,
                Architecture = argumentData.GetArm64() ? AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64 : AndroidArchitecture.ARMv7,
                TargetDevice = iOSTargetDevice.iPhoneAndiPad,
                IsAAB = argumentData.GetIsAAB(),
                SplitApplicationBinary = argumentData.GetSplitApp(),
                CreateSymbols = argumentData.GetCreateSymbols() ? AndroidCreateSymbols.Debugging : AndroidCreateSymbols.Disabled,
                IsDevelopmentBuild = argumentData.GetDebugBuild(),
            };

            var customCfg = new CustomBuildConfig
            {
                Enable = argumentData.GetEnableCustomBuild(),
                Data = argumentData.GetArgumentPack(),
            };

            var buildCfg = new ProjectBuildConfig(target, version, resCfg, appCfg, customCfg);
            var result = new ProjectBuilder().Build(buildCfg);
            return result.IsSuccess;
        }

        private static ArgumentData ParseArgument()
        {
            var args = Environment.GetCommandLineArgs();
            var argumentData = new ArgumentData();

            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (arg.StartsWith("-"))
                {
                    var name = arg.Substring(1);
                    var value = string.Empty;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        value = args[i + 1];
                        i++;
                    }
                    argumentData.AddArgument(name, value);
                }
            }
            
            return argumentData;
        }

        private class ArgumentData
        {
            private readonly Dictionary<string, string> _map = new Dictionary<string, string>();

            public ArgumentData()
            {
            }

            public Dictionary<string, object> GetArgumentPack()
            {
                var result = new Dictionary<string, object>();
                foreach (var (key, value) in _map)
                {
                    result.Add(key, value);
                }
                return result;
            }

            public void AddArgument(string key, string value)
            {
                if (!_map.TryAdd(key, value))
                {
                    _map.Add(key, value);
                }
            }

            private bool HasArgument(string key)
            {
                return _map.ContainsKey(key);
            }

            private string GetArgument(string key)
            {
                if (HasArgument(key))
                {
                    return _map[key];
                }
                
                return string.Empty;
            }

            private T GetEnumValue<T>(string key, T defaultValue) where T : struct
            {
                var argument = GetArgument(key);
                if (string.IsNullOrWhiteSpace(argument))
                {
                    return defaultValue;
                }
                
                if (Enum.TryParse<T>(argument, true, out var enumValue))
                {
                    return enumValue;
                }

                return defaultValue;
            }

            private bool GetBoolValue(string key, bool defaultValue)
            {
                var argument = GetArgument(key);
                if (string.IsNullOrWhiteSpace(argument))
                {
                    return defaultValue;
                }
                
                if (bool.TryParse(argument, out var boolValue))
                {
                    return boolValue;
                }

                return defaultValue;
            }

            private string GetStringValue(string key, string defaultValue)
            {
                var argument = GetArgument(key);
                if (string.IsNullOrWhiteSpace(argument))
                {
                    return defaultValue;
                }

                return argument;
            }

            private int GetIntValue(string key, int defaultValue)
            {
                var argument = GetArgument(key);
                if (string.IsNullOrWhiteSpace(argument))
                {
                    return defaultValue;
                }

                if (int.TryParse(argument, out var intValue))
                {
                    return intValue;
                }

                return defaultValue;
            }
            
#region Cutsom Argument
            public BuildTarget GetTarget()
            {
                return GetEnumValue("target", BuildTarget.NoTarget);
            }

            public bool GetEnableResBuild()
            {
                return GetBoolValue("enableRes", false);
            }

            public bool GetEnableAppBuild()
            {
                return GetBoolValue("enableApp", false);
            }

            public bool GetEnableCustomBuild()
            {
                return GetBoolValue("enableCustom", false);
            }

            public string GetAppVersion()
            {
                return GetStringValue("appVersion", PlayerSettings.bundleVersion);
            }

            public int GetAppCode()
            {
#if UNITY_ANDROID
                return GetIntValue("appCode", PlayerSettings.Android.bundleVersionCode);
#elif UNITY_IOS
                return GetIntValue("appCode", int.TryParse(PlayerSettings.iOS.buildNumber, out var buildCode) ? buildCode : 1);
#endif
                return 0;
            }
            
            public bool GetHashMode()
            {
                return GetBoolValue("hashMode", false);
            }

            public bool GetFlatMode()
            {
                return GetBoolValue("flatMode", false);
            }
            
            public bool GetArm64()
            {
#if UNITY_ANDROID
                return GetBoolValue("arm64", false);
#else
                return false;
#endif
            }

            public bool GetIsAAB()
            {
#if UNITY_ANDROID
                return GetBoolValue("isAAB", false);
#else
                return false;
#endif
            }

            public bool GetSplitApp()
            {
#if UNITY_ANDROID
                return GetBoolValue("splitApp", false);
#else
                return false;
#endif
            }
            
            public bool GetCreateSymbols()
            {
#if UNITY_ANDROID
                return GetBoolValue("createSymbols", false);
#else
                return false;
#endif
            }
            
            public bool GetDebugBuild()
            {
                return GetBoolValue("debugBuild", false);
            }
            

            public bool GetCopyToStreaming()
            {
                return GetBoolValue("copyToStreaming", false);
            }
#endregion
        }
    }
}