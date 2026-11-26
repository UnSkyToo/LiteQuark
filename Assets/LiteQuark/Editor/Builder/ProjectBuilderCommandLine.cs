using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    /// <summary>Unity batch-mode build entry point for <c>-executeMethod</c>.</summary>
    /// <remarks>
    /// <para><c>-target</c> is required and takes a <see cref="BuildTarget"/> name. Application builds support Android and iOS.</para>
    /// <para>Boolean options take <c>true</c> or <c>false</c> and default to <c>false</c>.
    /// <c>-enableApp</c> and <c>-enableRes</c> select the app and resource stages.
    /// <c>-enableCustom</c> sets the custom configuration passed to build callbacks.
    /// <c>-debugBuild</c> selects a Development Build.</para>
    /// <para><c>-appVersion</c> defaults to <see cref="PlayerSettings.bundleVersion"/>.
    /// <c>-appCode</c> defaults to the active Android or iOS build number (1 if the iOS value is not an integer).
    /// With any other active target, <c>-appCode</c> is ignored and the build code is 0.</para>
    /// <para><c>-hashMode</c> includes bundle hashes in published filenames, <c>-flatMode</c> flattens output paths,
    /// and <c>-copyToStreaming</c> replaces the LiteQuark resource output under StreamingAssets.</para>
    /// <para>When Android is the active build target, <c>-arm64</c> adds ARM64 alongside ARMv7,
    /// <c>-isAAB</c> builds an App Bundle, <c>-splitApp</c> enables Split Application Binary for App Bundles,
    /// and <c>-createSymbols</c> generates debugging symbols.</para>
    /// </remarks>
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
                IncrementalBuild = false,
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
                ProductName = PlayerSettings.productName,
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
            private readonly Dictionary<string, string> _map = new();

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

#region Custom Arguments
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
