using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    internal interface IBuildStep
    {
        string Name { get; }
        
        void Execute(ProjectBuilder builder);
    }

    internal class ProjectBuildResult
    {
        public bool IsSuccess { get; }
        public float ElapsedSeconds { get; }

        public ProjectBuildResult(bool isSuccess, float elapsedSeconds)
        {
            IsSuccess = isSuccess;
            ElapsedSeconds = elapsedSeconds;
        }
    }

    internal sealed class ProjectBuilder
    {
        public ProjectBuildConfig BuildConfig { get; private set; }
        
        public BuildTarget Target => BuildConfig.Target;
        public ResBuildConfig ResConfig => BuildConfig.ResConfig;
        public AppBuildConfig AppConfig => BuildConfig.AppConfig;

        private readonly List<string> Logs_ = new List<string>();

        public ProjectBuilder()
        {
        }

        public ProjectBuildResult Build(ProjectBuildConfig buildConfig)
        {
            BuildConfig = buildConfig;

            var stepList = GenerateBuildStep(buildConfig);
            
            return Execute(stepList);
        }

        private IBuildStep[] GenerateBuildStep(ProjectBuildConfig config)
        {
            var stepList = new List<IBuildStep>();
            
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
            steps.Add(new AppCleanStep());
            
            return steps.ToArray();
        }

        private ProjectBuildResult Execute(IBuildStep[] stepList)
        {
            Logs_.Clear();
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Log($"Start build");
            string error = null;
            
            foreach (var step in stepList)
            {
                var sw = new Stopwatch();
                sw.Start();
                try
                {
                    step.Execute(this);
                }
                catch (Exception ex)
                {
                    LogError($"{step.Name} error{Environment.NewLine}{ex.Message}");
                    error = ex.Message;
                }

                sw.Stop();
                Log($"{step.Name} {(string.IsNullOrEmpty(error) ? "success" : "failed")} with {sw.ElapsedMilliseconds / 1000f}s.");
                if (!string.IsNullOrEmpty(error))
                {
                    break;
                }
            }

            stopwatch.Stop();
            var isSuccess = string.IsNullOrEmpty(error);
            Log($"Complete build {(isSuccess ? "success" : $"failed({error})")} with {stopwatch.ElapsedMilliseconds / 1000f}s.");

            foreach (var log in Logs_)
            {
                LLogEditor.Info(log);
            }

            return new ProjectBuildResult(isSuccess, stopwatch.ElapsedMilliseconds / 1000f);
        }

        public void Log(string msg)
        {
            Logs_.Add(msg);
        }

        public void LogError(string msg)
        {
            LLogEditor.Error(msg);
        }
        
        public string GetResOutputPath()
        {
            return PathUtils.GetLiteQuarkRootPath($"Build/{Target}/Res");
        }
        
        public string GetAppOutputPath()
        {
            return PathUtils.GetLiteQuarkRootPath($"Build/{Target}/App");
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