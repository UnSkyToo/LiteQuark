using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class UnityUtilsEx
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return UnityUtils.GetOrAddComponent<T>(go);
        }

        public static T GetOrAddComponent<T>(this Transform go) where T : Component
        {
            return UnityUtils.GetOrAddComponent<T>(go);
        }
    }
    
    public static class UnityUtils
    {
        public static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (go == null)
            {
                return null;
            }

            var component = go.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return go.AddComponent<T>();
        }
        
        public static T GetOrAddComponent<T>(Transform go) where T : Component
        {
            return GetOrAddComponent<T>(go.gameObject);
        }
        
        public static GameObject CreateGameObject(Transform parent, string name)
        {
            var go = new GameObject(name);
            
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;

            return go;
        }

        public static void SetParent(Transform parent, GameObject child)
        {
            SetParent(parent, child.transform);
        }

        public static void SetParent(Transform parent, Transform child)
        {
            child.SetParent(parent, false);
            child.localPosition = Vector3.zero;
            child.localScale = Vector3.one;
            child.localRotation = Quaternion.identity;
        }
        
#if UNITY_EDITOR
        public static void ShowEditorNotification(string msg)
        {
            var func = typeof(UnityEditor.SceneView).GetMethod("ShowNotification", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            func?.Invoke(null, new object[] { msg });
        }
#endif
    }
}