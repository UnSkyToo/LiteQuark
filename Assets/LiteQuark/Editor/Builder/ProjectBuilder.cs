using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    internal sealed class ProjectBuilder
    {
        public ProjectBuildConfig BuildConfig { get; private set; }
        
        public BuildTarget Target => BuildConfig.Target;
        public ResBuildConfig ResConfig => BuildConfig.ResConfig;
        public AppBuildConfig AppConfig => BuildConfig.AppConfig;

        private IBuildCallback[] BuildCallbacks_ = Array.Empty<IBuildCallback>();

        public ProjectBuilder()
        {
        }

        public ProjectBuildReport Build(ProjectBuildConfig buildConfig)
        {
            BuildConfig = buildConfig;
            BuildCallbacks_ = ProjectBuilderUtils.CreateBuildCallbackInstance();
            
            var stepList = GenerateBuildStep(buildConfig);
            
            return Execute(stepList);
        }

        private IBuildStep[] GenerateBuildStep(ProjectBuildConfig config)
        {
            var stepList = new List<IBuildStep>();
            
            stepList.Add(new SwitchPlatformStep());
            
            var resStep = GenerateResBuildStep(ResConfig);
            stepList.AddRange(resStep);

            var appStep = GenerateAppBuildStep(AppConfig);
            stepList.AddRange(appStep);

            return stepList.ToArray();
        }

        private IBuildStep[] GenerateResBuildStep(ResBuildConfig config)
        {
            if (!config.Enable)
            {
                return Array.Empty<IBuildStep>();
            }
            
            var steps = new List<IBuildStep>();
            
            if (config.CleanBuildMode)
            {
                steps.Add(new ResCleanFileStep());
            }
            
            steps.Add(new ResCleanInfoStep());
            steps.Add(new ResCollectInfoStep());
            steps.Add(new ResBuildFileStep());
            steps.Add(new ResCleanInfoStep());

            if (config.CopyToStreamingAssets)
            {
                steps.Add(new ResCopyToStreamingAssetStep());
            }
            
            return steps.ToArray();
        }

        private IBuildStep[] GenerateAppBuildStep(AppBuildConfig config)
        {
            if (!config.Enable)
            {
                return Array.Empty<IBuildStep>();
            }

            var steps = new List<IBuildStep>();

            steps.Add(new AppPrepareStep());
            steps.Add(new AppGenerateProjectStep());
            steps.Add(new AppCompileProjectStep());
            // steps.Add(new AppCleanStep());
            
            return steps.ToArray();
        }

        private ProjectBuildReport Execute(IBuildStep[] stepList)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Log("Start build");
            string error = null;
            var buildReport = new ProjectBuildReport();
            buildReport.StartTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            
            PreProjectCallback();

            foreach (var step in stepList)
            {
                var sw = new Stopwatch();
                sw.Start();
                try
                {
                    PreStepCallback(step.GetType(), step.Name);
                    step.Execute(this);
                    PostStepCallback(step.GetType(), step.Name);
                }
                catch (Exception ex)
                {
                    buildReport.ErrorInfo = $"{step.Name} error{Environment.NewLine}{ex.Message}";
                    LogError(buildReport.ErrorInfo);
                    error = ex.Message;
                }

                sw.Stop();
                Log($"{step.Name} {(string.IsNullOrEmpty(error) ? "success" : "failed")} with {sw.ElapsedMilliseconds / 1000f}s.");
                if (!string.IsNullOrEmpty(error))
                {
                    break;
                }
            }
            
            PostProjectCallback();

            stopwatch.Stop();
            var isSuccess = string.IsNullOrEmpty(error);
            Log($"Complete build {(isSuccess ? "success" : $"failed({error})")} with {stopwatch.ElapsedMilliseconds / 1000f}s.");
            
            buildReport.IsSuccess = isSuccess;
            buildReport.ElapsedSeconds = stopwatch.ElapsedMilliseconds / 1000f;
            buildReport.OutputRootPath = GetRootOutputPath();
            buildReport.OutputResPath = GetResOutputPath();
            buildReport.OutputAppPath = GetAppOutputPath();
            buildReport.EndTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            
            return buildReport;
        }

        private void PreProjectCallback()
        {
            try
            {
                foreach (var callback in BuildCallbacks_)
                {
                    callback?.PreProjectBuild(BuildConfig);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }
        
        private void PostProjectCallback()
        {
            try
            {
                foreach (var callback in BuildCallbacks_)
                {
                    callback?.PostProjectBuild(BuildConfig);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private void PreStepCallback(Type stepType, string stepName)
        {
            try
            {
                foreach (var callback in BuildCallbacks_)
                {
                    callback?.PreStepBuild(BuildConfig, stepType, stepName);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }
        
        private void PostStepCallback(Type stepType, string stepName)
        {
            try
            {
                foreach (var callback in BuildCallbacks_)
                {
                    callback?.PostStepBuild(BuildConfig, stepType, stepName);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        public void Log(string msg)
        {
            LEditorLog.Info(msg);
        }

        public void LogError(string msg)
        {
            LEditorLog.Error(msg);
        }
        
        public string GetResOutputPath()
        {
            return $"{GetRootOutputPath()}/Res";
        }
        
        public string GetAppOutputPath()
        {
            return $"{GetRootOutputPath()}/App/{(AppConfig.IsDevelopmentBuild ? "Debug" : "Release")}";
        }

        public string GetRootOutputPath()
        {
            return PathUtils.GetLiteQuarkRootPath($"Build/{Target}");
        }

        public string GetIOSWorkspaceName()
        {
            return "XCodeProject";
        }
        
        public string[] GetBuildSceneList()
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
    }
}