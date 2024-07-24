﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
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

            var resCfg = new ResBuildConfig
            {
                Enable = argumentData.GetEnableResBuild(),
                CleanBuildMode = true,
                CopyToStreamingAssets = true,
                CleanStreamingAssetsBeforeCopy = true,
            };

            var appCfg = new AppBuildConfig
            {
                Enable = argumentData.GetEnableAppBuild(),
                CleanBuildMode = true,
                Version = argumentData.GetAppVersion(),
                BuildCode = argumentData.GetAppCode(),
                Backend = ScriptingImplementation.IL2CPP,
                Architecture = argumentData.GetArm64() ? AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64 : AndroidArchitecture.ARMv7,
                IsAAB = argumentData.GetIsAAB(),
                TargetDevice = iOSTargetDevice.iPhoneAndiPad,
                IsDevelopmentBuild = argumentData.GetDebugBuild(),
            };

            var buildCfg = new ProjectBuildConfig(target, resCfg, appCfg);
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
            private readonly Dictionary<string, string> Map_ = new Dictionary<string, string>();

            public ArgumentData()
            {
            }

            public void AddArgument(string key, string value)
            {
                if (!Map_.TryAdd(key, value))
                {
                    Map_.Add(key, value);
                }
            }

            private bool HasArgument(string key)
            {
                return Map_.ContainsKey(key);
            }

            private string GetArgument(string key)
            {
                if (HasArgument(key))
                {
                    return Map_[key];
                }

                Debug.LogError($"Argument {key} is not found.");
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
                if (HasArgument(key))
                {
                    return true;
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

            public string GetAppVersion()
            {
                return GetStringValue("version", PlayerSettings.bundleVersion);
            }

            public int GetAppCode()
            {
#if UNITY_ANDROID
                return GetIntValue("code", PlayerSettings.Android.bundleVersionCode);
#elif UNITY_IOS
                return GetIntValue("code", int.TryParse(PlayerSettings.iOS.buildNumber, out var buildCode) ? buildCode : 1);
#endif
                return 0;
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
                return GetBoolValue("isAAB", false);
            }

            public bool GetDebugBuild()
            {
                return GetBoolValue("debug", false);
            }
#endregion
        }
    }
}