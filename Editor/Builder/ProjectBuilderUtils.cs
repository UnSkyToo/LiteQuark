using LiteQuark.Runtime;
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

            var destPath = PathUtils.GetRuntimeRootPath();
            PathUtils.DeleteDirectory(destPath);
            PathUtils.CopyDirectory(resPath, destPath, CopyFilter);
            AssetDatabase.Refresh();
        }
    }
}