using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LiteBattle.Editor
{
    public static class LiteLayoutHelper
    {
        public const string DefaultLayoutName = "StateEditor";
        
        public static string GetLastLoadedLayoutName()
        {
            try
            {
                var type = Assembly.GetAssembly(typeof(EditorUtility)).GetType("UnityEditor.Toolbar");
                var getFunc = type.GetField("get");
                var nameInfo = type.GetField("m_LastLoadedLayoutName", BindingFlags.Instance | BindingFlags.NonPublic);
                var toolbarObj = getFunc?.GetValue(null);
                var name = nameInfo?.GetValue(toolbarObj);
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

        public static void OpenWorkspace()
        {
            string path = "Assets/StateEditor";
            EditorSceneManager.OpenScene(path);
            
            var defaultLayoutName = PlayerPrefs.GetString("LiteState.DefaultLayoutName", DefaultLayoutName);
            EditorUtility.LoadWindowLayout($"{path}/Layout/{defaultLayoutName}.wlt");
        }

        public static void SaveWorkspace()
        {
            var lastLoadName = GetLastLoadedLayoutName();
            if (lastLoadName.StartsWith(DefaultLayoutName))
            {
                PlayerPrefs.SetString("LiteState.DefaultLayoutName", lastLoadName);
            }
        }
    }
}