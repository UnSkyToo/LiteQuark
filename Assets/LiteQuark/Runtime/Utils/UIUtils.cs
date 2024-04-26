using System;
using UnityEngine;
using UnityEngine.UI;

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
            if (string.IsNullOrEmpty(path))
            {
                return parent;
            }
            
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
        
        public static Vector2 ScreenPosToCanvasPos(RectTransform parent, Vector2 screenPos, Camera camera = null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, camera, out var pos);
            return pos;
        }

        public static Vector2 ScreenPosToCanvasPos(Transform parent, Vector2 screenPos)
        {
            var rectTrans = parent.GetComponent<RectTransform>();
            if (rectTrans == null)
            {
                return Vector2.zero;
            }

            return ScreenPosToCanvasPos(rectTrans, screenPos);
        }

        public static Vector2 CanvasPosToScreenPos(RectTransform parent, Vector2 canvasPos)
        {
            var worldPos = CanvasPosToWorldPos(parent, canvasPos);
            return WorldPosToScreenPos(worldPos);
        }

        public static Vector2 CanvasPosToScreenPos(Transform parent, Vector2 canvasPos)
        {
            var rectTrans = parent.GetComponent<RectTransform>();
            if (rectTrans == null)
            {
                return Vector2.zero;
            }

            return CanvasPosToScreenPos(rectTrans, canvasPos);
        }

        public static Vector2 WorldPosToScreenPos(Vector3 worldPos, Camera camera = null)
        {
            return RectTransformUtility.WorldToScreenPoint(camera, worldPos);
        }

        public static Vector2 WorldPosToCanvasPos(RectTransform parent, Vector3 worldPos, Camera camera= null)
        {
            return ScreenPosToCanvasPos(parent, WorldPosToScreenPos(worldPos, camera));
        }

        public static Vector2 WorldPosToCanvasPos(Transform parent, Vector3 worldPos)
        {
            return ScreenPosToCanvasPos(parent, WorldPosToScreenPos(worldPos));
        }

        public static Vector3 CanvasPosToWorldPos(RectTransform parent, Vector2 canvasPos)
        {
            return parent.TransformPoint(canvasPos);
        }

        public static Vector3 CanvasPosToWorldPos(Transform parent, Vector2 canvasPos)
        {
            return parent.TransformPoint(canvasPos);
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

        public static Canvas AddSortingCanvas(GameObject go, int order)
        {
            var canvas = go.GetOrAddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;

            var raycaster = go.GetOrAddComponent<GraphicRaycaster>();
            raycaster.blockingMask = LayerMask.GetMask("UI");
            
            return canvas;
        }
        
        public static Transform CreatePanel(Transform parent, string name, int order)
        {
            var obj = new GameObject(name);
            var rectTrans = obj.AddComponent<RectTransform>();
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.anchoredPosition = Vector2.zero;
            rectTrans.sizeDelta = Vector2.zero;
            obj.layer = LayerMask.NameToLayer("UI");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<Canvas>().overrideSorting = true;
            obj.GetComponent<Canvas>().sortingOrder = order;
            obj.AddComponent<GraphicRaycaster>();
            return obj.transform;
        }
        
        public static void ReplaceSprite(GameObject go, string path, Sprite sprite)
        {
            var image = FindComponent<Image>(go, path);
            if (image != null)
            {
                image.sprite = sprite;
            }
        }

        public static void ReplaceSprite(GameObject go, string path, string resPath, bool async)
        {
            if (async)
            {
                LiteRuntime.Asset.LoadAssetAsync<Sprite>(resPath, (sprite) =>
                {
                    ReplaceSprite(go, path, sprite);
                });
            }
            else
            {
                var sprite = LiteRuntime.Asset.LoadAssetSync<Sprite>(resPath);
                ReplaceSprite(go, path, sprite);
            }
        }

        public static void ReplaceSprite(GameObject go, string resPath, bool async)
        {
            ReplaceSprite(go, null, resPath, async);
        }
    }
}