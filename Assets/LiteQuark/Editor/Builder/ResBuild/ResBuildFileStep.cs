﻿using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Build asset bundle
    /// </summary>
    internal sealed class ResBuildFileStep : IBuildStep
    {
        public string Name => "Res Build Step";

        public void Execute(ProjectBuilder builder)
        {
            var outputPath = builder.GetResOutputPath();
            PathUtils.CreateDirectory(outputPath);
            var buildOptions = builder.ResConfig.Options;
            buildOptions &= ~BuildAssetBundleOptions.AppendHashToAssetBundleName;
            builder.Collector.Manifest = BuildPipeline.BuildAssetBundles(outputPath, buildOptions, builder.Target);
            
            AssetDatabase.Refresh();
        }
    }
}