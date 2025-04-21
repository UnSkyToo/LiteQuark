using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class ProjectBuilderUtils
    {
        internal static void CopyToStreamingAssets(string resPath, bool removeOriginFolder)
        {
            bool CopyFilter(string path)
            {
                if (path.EndsWith(".manifest"))
                {
                    return false;
                }

                return true;
            }

            var destPath = PathUtils.ConcatPath(Application.streamingAssetsPath, LiteConst.Tag);
            if (removeOriginFolder)
            {
                PathUtils.DeleteDirectory(destPath);
            }

            PathUtils.CopyDirectory(resPath, destPath, CopyFilter);
            AssetDatabase.Refresh();
        }

        internal static IBuildCallback[] CreateBuildCallbackInstance()
        {
            try
            {
                var result = new List<IBuildCallback>();
                var typeList = TypeCache.GetTypesDerivedFrom<IBuildCallback>();

                foreach (var type in typeList)
                {
                    if (Activator.CreateInstance(type) is IBuildCallback instance)
                    {
                        result.Add(instance);
                    }
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                LEditorLog.Error(ex.Message);
                return Array.Empty<IBuildCallback>();
            }
        }
    }
}