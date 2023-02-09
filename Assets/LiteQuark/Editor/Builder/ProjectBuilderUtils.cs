using LiteQuark.Runtime;
using UnityEngine;
using UnityEditor;

namespace LiteQuark.Editor
{
    public static class ProjectBuilderUtils
    {
        public static void CopyToStreamingAssets(string resPath)
        {
            bool CopyFilter(string path)
            {
                if (path.EndsWith(".manifest"))
                {
                    return false;
                }

                return true;
            }
            
            PathUtils.DeleteDirectory(Application.streamingAssetsPath);
            PathUtils.CopyDirectory(resPath, Application.streamingAssetsPath, CopyFilter);
            AssetDatabase.Refresh();
        }
    }
}