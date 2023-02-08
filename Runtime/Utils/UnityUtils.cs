using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class UnityUtils
    {
        public static T GetOrCreateComponent<T>(GameObject go) where T : Component
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
        
        public static T GetOrCreateComponent<T>(Transform go) where T : Component
        {
            return GetOrCreateComponent<T>(go.gameObject);
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
    }
}