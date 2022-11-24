using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace LiteQuark.Editor
{
    public class AppBuilder
    {
        [MenuItem("Lite/Build/App")]
        private static void Func()
        {
            // new AppBuilder().Build(EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
            new AppBuilder().Build(BuildTarget.Android, BuildOptions.None);
        }

        public void Build(BuildTarget target, BuildOptions options)
        {
            CleanAppFile(target);
            
            // ApplySetting(target);
            
            BuildApp(target, options);
            
            LLogEditor.Info("Build App Success");
        }
        
        private void CleanAppFile(BuildTarget target)
        {
            PathHelper.DeleteDirectory(GetAppOutputPath(target));
            AssetDatabase.Refresh();
        }

        private void ApplySetting(BuildTarget target)
        {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            PlayerSettings.SetApplicationIdentifier(targetGroup, "com.lite.quark.demo");
            PlayerSettings.companyName = "lite";
            PlayerSettings.productName = "demo";
            PlayerSettings.bundleVersion = "1.0.0";

            EditorUserBuildSettings.development = false;
            
            AssetDatabase.SaveAssets();
        }
        
        private void BuildApp(BuildTarget target, BuildOptions options)
        {
            var outputPath = GetAppOutputPath(target);
            PathHelper.CreateDirectory(outputPath);

            var scenes = GetBuildSceneList();
            
            var buildSetting = new BuildPlayerOptions();
            buildSetting.target = target;
            buildSetting.options = options;
            buildSetting.locationPathName = outputPath;
            buildSetting.scenes = scenes;

            var report = BuildPipeline.BuildPlayer(buildSetting);

            if (report.summary.result == BuildResult.Failed)
            {
                LLogEditor.Info("Build App Failed");
            }
            
            AssetDatabase.Refresh();
        }

        private string[] GetBuildSceneList()
        {
            var names = new List<string>();
            
            foreach(var scene in EditorBuildSettings.scenes)
            {
                if (scene is { enabled: true }) 
                {
                    names.Add (scene.path);
                }
            }
            
            return names.ToArray();
        }
        
        private string GetAppOutputPath(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return PathHelper.GetLiteQuarkRootPath($"BuildApp/{target}/app_{PlayerSettings.bundleVersion}.apk");
                case BuildTarget.iOS:
                    return PathHelper.GetLiteQuarkRootPath($"BuildApp/{target}/XCodeProject");
            }

            return string.Empty;
        }
    }
}