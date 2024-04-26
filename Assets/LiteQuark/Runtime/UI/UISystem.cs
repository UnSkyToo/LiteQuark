﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public sealed class UISystem : ISystem, ITick
    {
        private const int UIStepOrder = 100;
        
        private readonly Dictionary<UIDepthMode, Transform> CanvasTransform_ = new Dictionary<UIDepthMode, Transform>();
        private readonly List<BaseUI> UIList_ = new List<BaseUI>();
        private readonly List<BaseUI> CloseList_ = new List<BaseUI>();

        public UISystem()
        {
            var parent = GameObject.Find("Canvas");
            if (parent == null)
            {
                parent = new GameObject("Canvas");
            }
            SetupCanvasRoot(parent);
            foreach (var depthModeName in Enum.GetNames(typeof(UIDepthMode)))
            {
                var depthMode = Enum.Parse<UIDepthMode>(depthModeName);
                var go = UnityUtils.CreateGameObject(parent.transform, $"Canvas_{depthModeName}");
                var rectTrans = go.AddComponent<RectTransform>();
                rectTrans.anchorMin = Vector2.zero;
                rectTrans.anchorMax = Vector2.one;
                rectTrans.anchoredPosition = Vector2.zero;
                rectTrans.sizeDelta = Vector2.zero;
                CanvasTransform_.Add(depthMode, go.transform);
            }
        }

        public void Dispose()
        {
            CloseAllUI();
            HandleCloseList();

            foreach (var item in CanvasTransform_)
            {
                GameObject.DestroyImmediate(item.Value.gameObject);
            }
            CanvasTransform_.Clear();
        }

        public void Tick(float deltaTime)
        {
            HandleCloseList();
            
            foreach (var ui in UIList_)
            {
                ui.Update(deltaTime);
            }
        }

        public T OpenUI<T>(params object[] paramList) where T : BaseUI
        {
            var ui = Activator.CreateInstance<T>();
            if (ui.IsMutex)
            {
                var existUI = FindUI<T>();
                if (existUI != null)
                {
                    return existUI;
                }
            }
            
            var parent = GetUIParent(ui.DepthMode);
            LiteRuntime.Asset.InstantiateAsync(ui.PrefabPath, parent, (instance) =>
            {
                if (instance == null)
                {
                    LLog.Error($"ui prefab load error : {ui.PrefabPath}");
                    return;
                }
                
                SetupUI(ui, instance);
                ui.Open(paramList);

                UIList_.Add(ui);
            });

            return ui;
        }

        public void CloseUI(BaseUI ui)
        {
            if (ui != null)
            {
                CloseList_.Add(ui);
            }
        }

        public void CloseUI<T>() where T : BaseUI
        {
            var ui = FindUI<T>();
            if (ui != null)
            {
                CloseUI(ui);
            }
        }

        private void CloseUIInternal(BaseUI ui)
        {
            UIBinder.AutoUnbind(ui);
            ui.Close();
            LiteRuntime.Asset.UnloadAsset(ui.Go);
        }

        public void CloseAllUI()
        {
            foreach (var ui in UIList_)
            {
                CloseUI(ui);
            }
        }

        private void HandleCloseList()
        {
            if (CloseList_.Count > 0)
            {
                foreach (var ui in CloseList_)
                {
                    CloseUIInternal(ui);
                    UIList_.Remove(ui);
                }
                CloseList_.Clear();
            }
        }

        public T FindUI<T>() where T : BaseUI
        {
            return UIList_.Find((ui) => ui.GetType() == typeof(T)) as T;
        }

        private void SetupUI(BaseUI ui, GameObject instance)
        {
            var parent = GetUIParent(ui.DepthMode);
            
            var rectTransform = instance.GetOrAddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var canvas = instance.GetOrAddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)ui.DepthMode + (parent.childCount - 1) * UIStepOrder;

            var raycaster = instance.GetOrAddComponent<GraphicRaycaster>();
            raycaster.blockingMask = LayerMask.GetMask("UI");
            
            ui.BindGo(instance);
            UIBinder.AutoBind(ui);
        }

        private Transform GetUIParent(UIDepthMode depthMode)
        {
            if (CanvasTransform_.TryGetValue(depthMode, out var parent))
            {
                return parent;
            }

            LLog.Error($"can't find canvas : {depthMode}");
            return null;
        }

        private void SetupCanvasRoot(GameObject go)
        {
            var rectTrans = go.GetOrAddComponent<RectTransform>();
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.anchoredPosition = Vector2.zero;
            rectTrans.sizeDelta = Vector2.zero;
            go.layer = LayerMask.NameToLayer("UI");
            
            // var canvas = go.GetOrAddComponent<Canvas>();
            // canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetOrAddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(LiteRuntime.Setting.UI.ResolutionWidth, LiteRuntime.Setting.UI.ResolutionHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
            scaler.referencePixelsPerUnit = 100;
            
            var raycaster = go.GetOrAddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
        }
    }
}