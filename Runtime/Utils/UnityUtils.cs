using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class UnityUtils
    {
        public static T GetComponentUpper<T>(Transform parent) where T : Component
        {
            while (parent != null)
            {
                var component = parent.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }

                parent = parent.parent;
            }

            return null;
        }
        
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

        public static GameObject CreateHoldGameObject(string name)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.NotEditable;
            Object.DontDestroyOnLoad(go);
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

        public static void ChangeLayer(GameObject parent, int layer)
        {
            parent.layer = layer;

            var children = parent.GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                child.gameObject.layer = layer;
            }
        }

        public static void ChangeSortingLayerName(GameObject parent, string layerName)
        {
            var renderers = parent.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.sortingLayerName = layerName;
            }
            
            var canvases = parent.GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.sortingLayerName = layerName;
            }
        }
        
        public static void ChangeSortingLayerID(GameObject parent, int layerID)
        {
            var renderers = parent.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.sortingLayerID = layerID;
            }
            
            var canvases = parent.GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.sortingLayerID = layerID;
            }
        }

        public static void ChangeSortingOrder(GameObject parent, int order)
        {
            var renderers = parent.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.sortingOrder = order;
            }
            
            var canvases = parent.GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.sortingOrder = order;
            }
        }

        public static void AddSortingOrder(GameObject parent, int order)
        {
            var renderers = parent.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.sortingOrder += order;
            }
            
            var canvases = parent.GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.sortingOrder += order;
            }
        }
        
#if UNITY_EDITOR
        public static void ShowEditorNotification(string msg)
        {
            var func = typeof(UnityEditor.SceneView).GetMethod("ShowNotification", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            func?.Invoke(null, new object[] { msg });
        }
        
        public static void SetResolution(int width, int height)
        {
            Screen.SetResolution(width, height, false);

            var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            var getMainGameViewFunc = gameViewType.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var gameView = getMainGameViewFunc.Invoke(null, null) as UnityEditor.EditorWindow;
            var gameViewSizeProp = gameView.GetType().GetProperty("currentGameViewSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var viewSize = gameViewSizeProp.GetValue(gameView, new object[0] { });
            var viewSizeType = viewSize.GetType();
            
            viewSizeType.GetProperty("width", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(viewSize, width, new object[0] { });
            viewSizeType.GetProperty("height", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).SetValue(viewSize, height, new object[0] { });

            var updateZoomAreaAndParentFunc = gameViewType.GetMethod("UpdateZoomAreaAndParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateZoomAreaAndParentFunc.Invoke(gameView, null);
        }
#endif
    }
    
    public static class UnityUtilsExtend
    {
        public static T GetComponentUpper<T>(this Transform parent) where T : Component
        {
            return UnityUtils.GetComponentUpper<T>(parent);
        }
        
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return UnityUtils.GetOrAddComponent<T>(go);
        }

        public static T GetOrAddComponent<T>(this Transform go) where T : Component
        {
            return UnityUtils.GetOrAddComponent<T>(go);
        }
        
        public static TaskAwaiter<AsyncOperation> GetAwaiter(this AsyncOperation asyncOperation)
        {
            var tcs = new TaskCompletionSource<AsyncOperation>();
            asyncOperation.completed += operation =>
            {
                tcs.SetResult(operation);
            };
            return tcs.Task.GetAwaiter();
        }
    }
}