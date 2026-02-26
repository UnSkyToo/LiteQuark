using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public static class UIUtils
    {
        public static Vector2 ScreenPointToCanvasPoint(RectTransform parent, Vector2 screenPoint, Camera uiCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, uiCamera, out var localPoint);
            return localPoint;
        }

        public static Vector2 ScreenPointToCanvasPoint(Transform parent, Vector2 screenPoint, Camera uiCamera)
        {
            var rt = parent.GetComponent<RectTransform>();
            if (rt == null)
            {
                return Vector2.zero;
            }

            return ScreenPointToCanvasPoint(rt, screenPoint, uiCamera);
        }

        public static Vector2 CanvasPointToScreenPoint(RectTransform parent, Vector2 canvasPoint, Camera worldCamera)
        {
            var worldPoint = CanvasPointToWorldPoint(parent, canvasPoint);
            return WorldPointToScreenPoint(worldPoint, worldCamera);
        }

        public static Vector2 CanvasPointToScreenPoint(Transform parent, Vector2 canvasPoint, Camera worldCamera)
        {
            var rt = parent.GetComponent<RectTransform>();
            if (rt == null)
            {
                return Vector2.zero;
            }

            return CanvasPointToScreenPoint(rt, canvasPoint, worldCamera);
        }

        public static Vector2 WorldPointToScreenPoint(Vector3 worldPoint, Camera worldCamera)
        {
            return RectTransformUtility.WorldToScreenPoint(worldCamera, worldPoint);
        }

        public static Vector2 WorldPointToCanvasPoint(Vector3 worldPoint, Camera worldCamera, RectTransform parent, Camera uiCamera)
        {
            return ScreenPointToCanvasPoint(parent, WorldPointToScreenPoint(worldPoint, worldCamera), uiCamera);
        }

        public static Vector2 WorldPointToCanvasPoint(Vector3 worldPoint, Camera worldCamera, Transform parent, Camera uiCamera)
        {
            return ScreenPointToCanvasPoint(parent, WorldPointToScreenPoint(worldPoint, worldCamera), uiCamera);
        }

        public static Vector3 CanvasPointToWorldPoint(RectTransform parent, Vector2 canvasPoint)
        {
            return parent.TransformPoint(canvasPoint);
        }

        public static Vector3 CanvasPointToWorldPoint(Transform parent, Vector2 canvasPoint)
        {
            return parent.TransformPoint(canvasPoint);
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

        public static void ReplaceSprite(Transform parent, string path, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }
            
            var image = UnityUtils.GetComponent<Image>(parent, path);
            if (image != null)
            {
                image.sprite = sprite;
                return;
            }

            var spriteRenderer = UnityUtils.GetComponent<SpriteRenderer>(parent, path);
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        public static void ReplaceSprite(GameObject parent, string path, Sprite sprite)
        {
            ReplaceSprite(parent.transform, path, sprite);
        }

        public static void ReplaceSprite(Transform parent, string path, string resPath)
        {
            LiteRuntime.Asset.LoadAssetAsync<Sprite>(resPath, (sprite) =>
            {
                ReplaceSprite(parent, path, sprite);
            });
        }

        public static void ReplaceSprite(GameObject parent, string path, string resPath)
        {
            ReplaceSprite(parent.transform, path, resPath);
        }

        public static void ReplaceSprite(Transform ts, string resPath)
        {
            ReplaceSprite(ts, null, resPath);
        }

        public static void ReplaceSprite(GameObject go, string resPath)
        {
            ReplaceSprite(go.transform, null, resPath);
        }
    }
}