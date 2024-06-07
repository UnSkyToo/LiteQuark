using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class BaseUI : BaseObject
    {
        public override string DebugName => $"UI<{DepthMode},{State}> - {PrefabPath}";

        public abstract string PrefabPath { get; }
        public abstract UIDepthMode DepthMode { get; }
        public abstract bool IsMutex { get; }
        
        public UIState State { get; set; }
        
        public GameObject Go { get; private set; }
        public RectTransform RT { get; private set; }

        public int SortingOrder => Go.GetComponent<Canvas>().sortingOrder;

        private readonly int EventTag_;

        protected BaseUI()
            : base()
        {
            EventTag_ = GetType().Name.GetHashCode();
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
            UnRegisterAllEvent();
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

            var canvasSize = LiteRuntime.UI.CanvasRoot.sizeDelta;

            OnAdaptAnchorsValue(canvasSize, anchorMin, anchorMax);
        }

        protected virtual void OnAdaptAnchorsValue(Vector2 canvasSize, Vector2 anchorMin, Vector2 anchorMax)
        {
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

        /// <summary>
        /// 注册事件，UI会在关闭后自动反注册
        /// </summary>
        protected void RegisterEvent<T>(Action<T> callback) where T : BaseEvent
        {
            LiteRuntime.Event.Register(EventTag_, callback);
        }

        /// <summary>
        /// 手动反注册事件
        /// </summary>
        protected void UnRegisterEvent<T>(Action<T> callback) where T : BaseEvent
        {
            LiteRuntime.Event.UnRegister(EventTag_, callback);
        }
        
        private void UnRegisterAllEvent()
        {
            LiteRuntime.Event.UnRegisterAll(EventTag_);
        }
    }
}