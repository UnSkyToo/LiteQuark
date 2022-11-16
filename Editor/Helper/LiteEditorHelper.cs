using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
#if UNITY_EDITOR
    public static class LiteEditorHelper
    {
        public static void OpenFolder(string path)
        {
#if UNITY_EDITOR_WIN
            Application.OpenURL(path);
#elif UNITY_STANDALONE_OSX
            EditorUtility.RevealInFinder(path);
#endif
        }

        public static void Ping(string path)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            Ping(obj);
        }
        
        public static void Ping(Object obj)
        {
            if (obj == null)
            {
                return;
            }
            
            
            if (obj is DefaultAsset)
            {
                var browserType = typeof(EditorWindow).Assembly.GetType("ProjectBrowser");
                if (browserType != null)
                {
                    var instance = browserType.GetField("s_LastInteractedProjectBrowser", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetValue(null);
                    var setFunc = browserType.GetMethod("SetFolderSelection", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    var viewMode = (int)browserType.GetField("m_ViewMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetValue(instance);
                    if (viewMode == 0)
                    {
                        Selection.activeObject = obj;
                    }
                    else if (setFunc != null)
                    {
                        setFunc.Invoke(instance, new object[] { new[] { obj.GetInstanceID() }, false });
                    }
                }
            }
            else
            {
                EditorGUIUtility.PingObject(obj);
            }
        }

        public static void OpenAsset(string path)
        {
            if (PathHelper.PathIsFile(path))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (obj != null)
                {
                    AssetDatabase.OpenAsset(obj);
                }
            }
            else
            {
                OpenFolder(path);
            }
        }
    }
}
#endif