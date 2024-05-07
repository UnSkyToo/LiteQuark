using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class BaseUI
    {
        public abstract string PrefabPath { get; }
        public abstract UIDepthMode DepthMode { get; }
        public abstract bool IsMutex { get; }
        
        public UIState State { get; set; }
        
        public GameObject Go { get; private set; }
        public RectTransform RT { get; private set; }

        public int SortingOrder => Go.GetComponent<Canvas>().sortingOrder;

        protected BaseUI()
        {
        }

        public void BindGo(GameObject go)
        {
            Go = go;
            RT = Go.GetComponent<RectTransform>();
        }

        public void Open(params object[] paramList)
        {
            AdaptAnchorsValue();
            OnOpen(paramList);
        }

        public void Close()
        {
            OnClose();
        }

        public void Update(float deltaTime)
        {
            OnUpdate(deltaTime);
        }
        
        protected virtual void OnOpen(params object[] paramList)
        {
        }
        
        protected virtual void OnClose()
        {
        }

        protected virtual void OnUpdate(float deltaTime)
        {
        }

        private void AdaptAnchorsValue()
        {
            var maxWidth = Display.main.systemWidth;
            var maxHeight = Display.main.systemHeight;
            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= maxWidth;
            anchorMin.y /= maxHeight;
            anchorMax.x /= maxWidth;
            anchorMax.y /= maxHeight;

            RT.anchorMin = anchorMin;
            RT.anchorMax = anchorMax;
        }

        public Transform FindChild(string path)
        {
            return UIUtils.FindChild(Go, path);
        }

        public Component FindComponent(string path, Type type)
        {
            return UIUtils.FindComponent(Go, path, type);
        }

        public T FindComponent<T>(string path) where T : Component
        {
            return UIUtils.FindComponent<T>(Go, path);
        }

        public void SetActive(string path, bool value)
        {
            UIUtils.SetActive(Go, path, value);
        }
    }
}