using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
#if UNITY_EDITOR
    public static class LiteEditorUtils
    {
        public static void OpenFolder(string path)
        {
#if UNITY_EDITOR_WIN
            Application.OpenURL(path);
#elif UNITY_STANDALONE_OSX
            EditorUtility.RevealInFinder(path);
#endif
        }

        public static void GenerateCSharpProject()
        {
            var editorType = Unity.CodeEditor.CodeEditor.Editor.CurrentCodeEditor.GetType();
            var generationVal = editorType.GetField("m_ProjectGeneration", BindingFlags.Static | BindingFlags.NonPublic);
            if (generationVal == null)
            {
                Debug.LogWarning($"can't find m_ProjectGeneration field in {editorType}");
                return;
            }
            var generationObj = generationVal.GetValue(Unity.CodeEditor.CodeEditor.Editor.CurrentCodeEditor);
            if (generationObj == null)
            {
                Debug.LogWarning("can't get m_ProjectGeneration instance value");
                return;
            }
            
            var generationValType = generationVal.FieldType;
            var syncMethod = generationValType.GetMethod("Sync", BindingFlags.Instance | BindingFlags.Public);
            if (syncMethod == null)
            {
                Debug.LogWarning($"can't get Sync method in {generationValType}");
                return;
            }
            
            syncMethod.Invoke(generationObj, null);
            
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
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
                var browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                if (browserType != null)
                {
                    var instance = browserType.GetField("s_LastInteractedProjectBrowser", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    var setFunc = TypeUtils.GetMethod(browserType, "SetFolderSelection", 2, BindingFlags.NonPublic | BindingFlags.Instance);
                    // var setFunc = browserType.GetMethod("SetFolderSelection", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var viewMode = (int)browserType.GetField("m_ViewMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
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
            if (PathUtils.PathIsFile(path))
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