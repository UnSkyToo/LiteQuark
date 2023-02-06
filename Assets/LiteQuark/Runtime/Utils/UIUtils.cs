using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class UIUtils
    {
        public static Transform FindChild(GameObject parent, string path)
        {
            return FindChild(parent.transform, path);
        }
        
        public static Transform FindChild(Transform parent, string path)
        {
            return parent.Find(path); 
        }

        public static Component FindComponent(GameObject parent, string path, Type type)
        {
            return FindComponent(parent.transform, path, type);
        }

        public static Component FindComponent(Transform parent, string path, Type type)
        {
            var child = FindChild(parent, path);

            if (child != null)
            {
                return child.GetComponent(type);
            }

            return null;
        }

        public static T FindComponent<T>(GameObject parent, string path) where T : Component
        {
            return FindComponent<T>(parent.transform, path);
        }
        
        public static T FindComponent<T>(Transform parent, string path) where T : Component
        {
            var child = FindChild(parent, path);
            
            if (child != null)
            {
                return child.GetComponent<T>();
            }

            return null;
        }

        public static T FindComponentUpper<T>(GameObject current) where T : Component
        {
            return FindComponentUpper<T>(current.transform);
        }

        public static T FindComponentUpper<T>(Transform current) where T : Component
        {
            while (current != null)
            {
                var component = current.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
                current = current.parent;
            }

            return null;
        }

        public static void SetActive(GameObject parent, string path, bool value)
        {
            SetActive(parent.transform, path, value);
        }

        public static void SetActive(Transform parent, string path, bool value)
        {
            var child = FindChild(parent, path);
            if (child != null)
            {
                child.gameObject.SetActive(value);
            }
        }
    }
}