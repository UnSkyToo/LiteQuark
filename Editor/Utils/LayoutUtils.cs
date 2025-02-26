#if UNITY_EDITOR
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LiteQuark.Editor
{
    public static class LayoutUtils
    {
        public const string DefaultLayoutPrefsKey = "LiteQuark.DefaultLayoutName";
        public const string DefaultLayoutName = "LiteQuark.Layout";
        
        public static string GetLastLoadedLayoutName()
        {
            try
            {
                var type = Assembly.GetAssembly(typeof(EditorUtility)).GetType("UnityEditor.Toolbar");
                var toolbarObj = ReflectionUtils.GetFieldValue(type, "get");
                var name = ReflectionUtils.GetFieldValue(type, toolbarObj, "m_LastLoadedLayoutName", BindingFlags.Instance | BindingFlags.NonPublic);
                if (name == null)
                {
                    return DefaultLayoutName;
                }

                return (string) name;
            }
            catch (System.Exception)
            {
                // ignore
            }

            return DefaultLayoutName;
        }

        public static void OpenWorkspace(string scenePath, string layoutPath)
        {
            EditorSceneManager.OpenScene(scenePath);
            
            var defaultLayoutName = PlayerPrefs.GetString(DefaultLayoutPrefsKey, DefaultLayoutName);
            EditorUtility.LoadWindowLayout($"{layoutPath}/{defaultLayoutName}.wlt");
        }

        public static void SaveWorkspace()
        {
            var lastLoadName = GetLastLoadedLayoutName();
            if (lastLoadName.StartsWith(DefaultLayoutName))
            {
                PlayerPrefs.SetString(DefaultLayoutPrefsKey, lastLoadName);
            }
        }
    }
}
#endif