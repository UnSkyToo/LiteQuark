using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LiteBattle.Editor
{
    public static class LiteStateUtils
    {
        public static void OpenStateEditorScene()
        {
            // 搜索所有名为 "LiteStateEditorScene.scene" 的文件
            var guids = AssetDatabase.FindAssets("LiteStateEditorScene t:Scene");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("LiteStateEditorScene"))
                {
                    EditorSceneManager.OpenScene(path);
                    return;
                }
            }

            UnityEngine.Debug.LogError("无法找到 LiteStateEditorScene.scene 文件！");
        }

        public static string GetRelativePath(string fullPath)
        {
            var relativePath = fullPath;
            relativePath = relativePath.Replace(LiteStateConfig.Instance.RootPath, string.Empty);
            return relativePath;
        }

        public static string GetFullPath(string subPath)
        {
            return PathUtils.ConcatPath(LiteStateConfig.Instance.RootPath, subPath);
        }
        
        private static string GetConfigFullPath(string subPath)
        {
            return PathUtils.ConcatPath(LiteStateConfig.Instance.DataPath, subPath);
        }

        public static string GetAgentRootPath()
        {
            return GetConfigFullPath("Agent");
        }

        public static string GetTimelineRootPath()
        {
            return GetConfigFullPath("Timeline");
        }
    }
}