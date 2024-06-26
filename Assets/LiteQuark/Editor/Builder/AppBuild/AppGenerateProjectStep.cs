﻿using System;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Generate project
    /// </summary>
    internal sealed class AppGenerateProjectStep : IBuildStep
    {
        public string Name => "App Generate Project Step";

        public void Execute(ProjectBuilder builder)
        {
            var outputPath = builder.GetAppOutputPath();
            PathUtils.CreateDirectory(outputPath);
            
            ApplySetting(builder);
            
            switch (builder.Target)
            {
                case BuildTarget.Android:
                    BuildAndroid(builder);
                    break;
                case BuildTarget.iOS:
                    BuildIOS(builder);
                    break;
                default:
                    builder.LogError($"unsupported platform : {builder.Target}");
                    break;
            }
        }

        private void ApplySetting(ProjectBuilder builder)
        {
            PlayerSettings.applicationIdentifier = builder.AppConfig.Identifier;
            PlayerSettings.bundleVersion = builder.AppConfig.Version;
            PlayerSettings.productName = builder.AppConfig.ProduceName;
#if UNITY_ANDROID
            PlayerSettings.Android.bundleVersionCode = builder.AppConfig.BuildCode;
#endif
        }

        private void BuildAndroid(ProjectBuilder builder)
        {
            var time = DateTime.Now;
            var apkName = $"{builder.AppConfig.ProduceName}_{builder.AppConfig.Version}_{builder.AppConfig.BuildCode}_{time.Hour}_{time.Minute}.apk";

            var buildSetting = new BuildPlayerOptions();
            buildSetting.target = BuildTarget.Android;
            buildSetting.targetGroup = BuildTargetGroup.Android;
            buildSetting.options = builder.AppConfig.Options;
            buildSetting.locationPathName = $"{builder.GetAppOutputPath()}/{apkName}";
            buildSetting.scenes = builder.GetBuildSceneList();

            var report = BuildPipeline.BuildPlayer(buildSetting);

            if (report.summary.result == BuildResult.Failed)
            {
                builder.LogError("Build App Failed");
            }
            
            AssetDatabase.Refresh();
        }

        private void BuildIOS(ProjectBuilder builder)
        {
            var buildSetting = new BuildPlayerOptions();
            buildSetting.target = BuildTarget.iOS;
            buildSetting.targetGroup = BuildTargetGroup.iOS;
            buildSetting.options = builder.AppConfig.Options;
            buildSetting.locationPathName = $"{builder.GetAppOutputPath()}/XCodeProject";
            buildSetting.scenes = builder.GetBuildSceneList();

            var report = BuildPipeline.BuildPlayer(buildSetting);

            if (report.summary.result == BuildResult.Failed)
            {
                builder.LogError("Build App Failed");
            }
            
            AssetDatabase.Refresh();
        }
    }
}