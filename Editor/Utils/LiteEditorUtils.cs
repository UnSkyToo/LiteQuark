#if UNITY_EDITOR
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LiteQuark.Editor
{
    public static class LiteEditorUtils
    {
        public static void OpenFolder(string path)
        {
#if UNITY_EDITOR_WIN
            Application.OpenURL(path);
#elif UNITY_EDITOR_OSX
            EditorUtility.RevealInFinder(path);
#endif
        }

        public static void GenerateCSharpProject()
        {
            var generationObj = ReflectionUtils.GetFieldValue(Unity.CodeEditor.CodeEditor.Editor.CurrentCodeEditor, "m_ProjectGeneration", BindingFlags.Static | BindingFlags.NonPublic);
            ReflectionUtils.InvokeMethod(generationObj, "Sync", BindingFlags.Instance | BindingFlags.Public);

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
                    var setFunc = ReflectionUtils.GetMethod(browserType, "SetFolderSelection", 2, BindingFlags.NonPublic | BindingFlags.Instance);
                    // var setFunc = browserType.GetMethod("SetFolderSelection", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var viewMode = (int)browserType.GetField("m_ViewMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
                    if (viewMode == 0)
                    {
                        Selection.activeObject = obj;
                    }
                    else if (setFunc != null)
                    {
                        try
                        {
                            setFunc.Invoke(instance, new object[] { new[] { obj.GetInstanceID() }, false });
                        }
                        catch
                        {
                            // ignored
                        }
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
        
        public static string GetSelectedFolderPath()
        {
            if (Selection.activeObject == null)
            {
                return string.Empty;
            }

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(path))
            {
                return path;
            }
            
            return string.Empty;
        }

        public static NamedBuildTarget GetNamedBuildTarget(BuildTarget target)
        {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            return namedBuildTarget;
        }

        public static void AddScriptingDefineSymbols(string addSymbol)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            AddScriptingDefineSymbols(target, addSymbol);
        }

        public static void AddScriptingDefineSymbols(BuildTarget target, string addSymbol)
        {
            var namedBuildTarget = GetNamedBuildTarget(target);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            if (!symbols.Contains(addSymbol))
            {
                symbols += $";{addSymbol}";
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, symbols);
            }
        }
        
        public static void RemoveScriptingDefineSymbols(string removeSymbol)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            RemoveScriptingDefineSymbols(target, removeSymbol);
        }

        public static void RemoveScriptingDefineSymbols(BuildTarget target, string removeSymbol)
        {
            var namedBuildTarget = GetNamedBuildTarget(target);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            
            if (symbols.Contains(removeSymbol))
            {
                symbols = symbols.Replace(removeSymbol, string.Empty).Replace(";;", ";").Trim(';');
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, symbols);
            }
        }
    }
}
#endif