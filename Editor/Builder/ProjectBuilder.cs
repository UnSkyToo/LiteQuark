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
        public string Version => BuildConfig.Version;
        public ResBuildConfig ResConfig => BuildConfig.ResConfig;
        public AppBuildConfig AppConfig => BuildConfig.AppConfig;
        public ResCollector Collector { get; private set; }
        
        private IBuildCallback[] _buildCallbacks = Array.Empty<IBuildCallback>();

        public ProjectBuilder()
        {
        }

        public ProjectBuildReport Build(ProjectBuildConfig buildConfig)
        {
            BuildConfig = buildConfig;
            Collector = new ResCollector();
            _buildCallbacks = ProjectBuilderUtils.CreateBuildCallbackInstance();
            
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

            if (!config.IncrementBuildModel)
            {
                steps.Add(new ResCleanFileStep());
            }
            
            if (config.CleanBuildMode)
            {
                steps.Add(new ResCleanInfoStep());
            }
            
            steps.Add(new ResCollectInfoStep());
            steps.Add(new ResBuildFileStep());
            steps.Add(new ResGeneratePackInfoStep());

            if (config.CleanBuildMode)
            {
                steps.Add(new ResCleanInfoStep());
            }

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
            
            PlayerSettings.bundleVersion = Version;
            
            PreProjectCallback();

            foreach (var step in stepList)
            {
                var sw = new Stopwatch();
                sw.Start();
                Log($"<{step.Name}> start.");
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
                Log($"<{step.Name}> over. status: {(string.IsNullOrEmpty(error) ? "success" : "failed")} with {sw.ElapsedMilliseconds / 1000f}s.");
                if (!string.IsNullOrEmpty(error))
                {
                    break;
                }
            }
            
            stopwatch.Stop();
            var isSuccess = string.IsNullOrEmpty(error);
            Log($"Complete build {(isSuccess ? "success" : $"failed({error})")} with {stopwatch.ElapsedMilliseconds / 1000f}s.");
            
            buildReport.IsSuccess = isSuccess;
            buildReport.ElapsedSeconds = stopwatch.ElapsedMilliseconds / 1000f;
            buildReport.OutputRootPath = GetRootOutputPath();
            buildReport.OutputResPath = GetResOutputPath();
            buildReport.OutputAppPath = GetAppOutputPath();
            buildReport.EndTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            
            PostProjectCallback(buildReport);
            
            return buildReport;
        }

        private void PreProjectCallback()
        {
            try
            {
                foreach (var callback in _buildCallbacks)
                {
                    callback?.PreProjectBuild(BuildConfig);
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }
        
        private void PostProjectCallback(ProjectBuildReport report)
        {
            try
            {
                foreach (var callback in _buildCallbacks)
                {
                    callback?.PostProjectBuild(BuildConfig, report);
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
                foreach (var callback in _buildCallbacks)
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
                foreach (var callback in _buildCallbacks)
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
            LEditorLog.Info($"[{LiteConst.Tag}] {msg}");
        }

        public void LogError(string msg)
        {
            LEditorLog.Error($"[{LiteConst.Tag}] {msg}");
        }
        
        public string GetResOutputPath()
        {
            return $"{GetRootOutputPath()}/Res";
        }
        
        public string GetAppOutputPath()
        {
            return $"{GetRootOutputPath()}/App";
        }

        public string GetRootOutputPath()
        {
            return PathUtils.GetLiteQuarkRootPath($"Build/{Target}/{Version}/{(AppConfig.IsDevelopmentBuild ? "Debug" : "Release")}");
        }

        public string GetIOSWorkspaceName()
        {
            return "XCodeProject";
        }

        public string GetLegalProduceName()
        {
            var legalProduceName = AppConfig.ProduceName;
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                legalProduceName = legalProduceName.Replace(c, '_');
            }

            return legalProduceName.Replace(' ', '_');
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