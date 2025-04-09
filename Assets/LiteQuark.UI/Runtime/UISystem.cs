using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime.UI
{
    public sealed class UISystem : ISystem, ITick
    {
        private const int UIStepOrder = 100;

        public RectTransform CanvasRoot => CanvasRoot_;
        public Camera UICamera => LiteRuntime.Setting.UI.UICamera ?? Camera.main;

        private RectTransform CanvasRoot_;
        private readonly Dictionary<UIDepthMode, Transform> CanvasTransform_ = new Dictionary<UIDepthMode, Transform>();
        private readonly List<BaseUI> OpenList_ = new List<BaseUI>();
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
        
        public Task<bool> Initialize()
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            HandleOpenList();
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
            HandleOpenList();
            HandleCloseList();
            
            foreach (var ui in UIList_)
            {
                ui.Update(deltaTime);
            }
        }

        public void OpenUI<T>(UIConfig config, params object[] paramList) where T : BaseUI
        {
            if (config.IsMutex)
            {
                var existUI = FindUI<T>();
                if (existUI != null)
                {
                    return;
                }
            }

            var ui = Activator.CreateInstance<T>();
            ui.Config = config;
            ui.System = this;
            ui.State = UIState.Opening;
            var parent = GetUIParent(config.DepthMode);
            LiteRuntime.Asset.InstantiateAsync(config.PrefabPath, parent, (instance) =>
            {
                if (instance == null)
                {
                    ui.State = UIState.Error;
                    LLog.Error($"ui prefab load error : {config.PrefabPath}");
                    return;
                }
                
                SetupUI(ui, instance);
                ui.Open(paramList);

                ui.State = UIState.Opened;
                OpenList_.Add(ui);
            });
        }

        private void HandleOpenList()
        {
            if (OpenList_.Count > 0)
            {
                foreach (var ui in OpenList_)
                {
                    UIList_.Add(ui);
                }
                OpenList_.Clear();
            }
        }

        public void CloseUI(BaseUI ui)
        {
            if (ui != null && !CloseList_.Contains(ui))
            {
                ui.State = UIState.Closing;
                ui.Close();
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
            ui.State = UIState.Closed;
            UIBinder.AutoUnbind(ui);
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
            return UIList_.Find((ui) => ui.GetType() == typeof(T) && (ui.State is UIState.Opening or UIState.Opened)) as T;
        }

        private void SetupUI(BaseUI ui, GameObject instance)
        {
            var parent = GetUIParent(ui.Config.DepthMode);
            
            var rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = instance.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }

            var canvas = instance.GetOrAddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)ui.Config.DepthMode + (parent.childCount - 1) * (ui.Config.DepthMode == UIDepthMode.Scene ? 0 : UIStepOrder);

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
            // rectTrans.anchorMin = Vector2.zero;
            // rectTrans.anchorMax = Vector2.one;
            // rectTrans.anchoredPosition = Vector2.zero;
            // rectTrans.sizeDelta = Vector2.zero;
            go.layer = LayerMask.NameToLayer("UI");
            
            var canvas = go.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UICamera;

            var scaler = go.GetOrAddComponent<CanvasScaler>();
            scaler.uiScaleMode = LiteRuntime.Setting.UI.ScaleMode;
            scaler.screenMatchMode = LiteRuntime.Setting.UI.MatchMode;
            scaler.referenceResolution = new Vector2(LiteRuntime.Setting.UI.ResolutionWidth, LiteRuntime.Setting.UI.ResolutionHeight);
            scaler.matchWidthOrHeight = LiteRuntime.Setting.UI.MatchValue;
            scaler.referencePixelsPerUnit = 100;
            
            UICamera.orthographic = true;
            UICamera.orthographicSize = Screen.height / (2f * scaler.referencePixelsPerUnit);
            
            var raycaster = go.GetOrAddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;

            CanvasRoot_ = rectTrans;
        }
    }
}