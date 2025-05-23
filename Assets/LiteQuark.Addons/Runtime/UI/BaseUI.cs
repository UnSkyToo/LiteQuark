﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class BaseUI : BaseObject
    {
        public override string DebugName => $"UI<{Config.DepthMode},{State}> - {Config.PrefabPath}";

        public UIConfig Config { get; set; }
        public UISystem System { get; set; }
        public UIState State { get; set; }
        
        public GameObject Go { get; private set; }
        public RectTransform RT { get; private set; }

        public int SortingOrder { get; private set; }

        private readonly List<Sprite> _loadSpriteList = new List<Sprite>();
        private readonly int _eventTag;

        protected BaseUI()
            : base()
        {
            _eventTag = GetType().Name.GetHashCode();
        }

        public void BindGo(GameObject go)
        {
            Go = go;
            RT = Go.GetComponent<RectTransform>();
            SortingOrder = Go.GetComponent<Canvas>().sortingOrder;
        }

        public void Open(params object[] paramList)
        {
            AdaptAnchorsValue();
            OnOpen(paramList);
        }

        public void Close()
        {
            UnloadSprites();
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
            if (!Config.AutoAdapt)
            {
                return;
            }
            
            var maxWidth = Display.main.systemWidth;
            var maxHeight = Display.main.systemHeight;
            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= maxWidth;
            anchorMin.y /= maxHeight;
            anchorMax.x /= maxWidth;
            anchorMax.y /= maxHeight;

            var canvasSize = System.CanvasRoot.sizeDelta;

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
        
        public Component FindComponent(Type type)
        {
            return Go.GetComponent(type);
        }
        
        public T FindComponent<T>(string path) where T : Component
        {
            return UIUtils.FindComponent<T>(Go, path);
        }
        
        public T FindComponent<T>() where T : Component
        {
            return Go.GetComponent<T>();
        }

        public void SetActive(string path, bool value)
        {
            UIUtils.SetActive(Go, path, value);
        }
        
        public void LoadSprite(string resPath, Action<Sprite> callback)
        {
            LiteRuntime.Asset.LoadAssetAsync<Sprite>(resPath, (sprite) =>
            {
                _loadSpriteList.Add(sprite);
                callback?.Invoke(sprite);
            });
        }
        
        public void ReplaceSprite(string path, string resPath)
        {
            LoadSprite(resPath, (sprite) =>
            {
                UIUtils.ReplaceSprite(Go, path, sprite);
            });
        }
        
        public void ReplaceSprite(Transform parent, string path, string resPath)
        {
            LoadSprite(resPath, (sprite) =>
            {
                UIUtils.ReplaceSprite(parent, path, sprite);
            });
        }
        
        public void ReplaceSprite(GameObject parent, string path, string resPath)
        {
            ReplaceSprite(parent.transform, path, resPath);
        }

        private void UnloadSprites()
        {
            foreach (var sprite in _loadSpriteList)
            {
                LiteRuntime.Asset.UnloadAsset(sprite);
            }
            _loadSpriteList.Clear();
        }

        /// <summary>
        /// 注册事件，UI会在关闭后自动反注册
        /// </summary>
        protected void RegisterEvent<T>(Action<T> callback) where T : IEventData
        {
            LiteRuntime.Event.Register(_eventTag, callback);
        }

        /// <summary>
        /// 手动反注册事件
        /// </summary>
        protected void UnRegisterEvent<T>(Action<T> callback) where T : IEventData
        {
            LiteRuntime.Event.UnRegister(_eventTag, callback);
        }
        
        private void UnRegisterAllEvent()
        {
            LiteRuntime.Event.UnRegisterAll(_eventTag);
        }
    }
}