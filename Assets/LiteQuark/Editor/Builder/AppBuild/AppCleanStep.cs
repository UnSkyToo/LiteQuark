﻿using LiteQuark.Runtime;
using UnityEngine;
using UnityEditor;

namespace LiteQuark.Editor
{
    /// <summary>
    /// Clean streamingAssets
    /// </summary>
    internal sealed class AppCleanStep : IBuildStep
    {
        public string Name => "App Clean Step";

        public void Execute(ProjectBuilder builder)
        {
            FileUtil.DeleteFileOrDirectory(Application.streamingAssetsPath);
            FileUtil.DeleteFileOrDirectory(PathHelper.ConcatPath(Application.dataPath, "StreamingAssets.meta"));
            AssetDatabase.Refresh();
        }
    }
}