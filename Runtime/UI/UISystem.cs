using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public sealed class UISystem : ISystem, ITick
    {
        private const int UIStepOrder = 100;

        public RectTransform CanvasRoot => _canvasRoot;
        public Camera UICamera => LiteRuntime.Setting.UI.UICamera ?? Camera.main;

        private RectTransform _canvasRoot;
        private readonly Dictionary<UIDepthMode, Transform> _canvasTransform = new Dictionary<UIDepthMode, Transform>();
        private readonly List<BaseUI> _openList = new List<BaseUI>();
        private readonly List<BaseUI> _uiList = new List<BaseUI>();
        private readonly List<BaseUI> _closeList = new List<BaseUI>();

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
                _canvasTransform.Add(depthMode, go.transform);
            }
        }
        
        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            HandleOpenList();
            CloseAllUI();
            HandleCloseList();

            foreach (var item in _canvasTransform)
            {
                UnityEngine.Object.Destroy(item.Value.gameObject);
            }
            _canvasTransform.Clear();
        }

        public void Tick(float deltaTime)
        {
            HandleOpenList();
            HandleCloseList();
            
            foreach (var ui in _uiList)
            {
                ui.Update(deltaTime);
            }
        }

        public UniTask<T> OpenUI<T>(UIConfig config, params object[] paramList) where T : BaseUI
        {
            if (config.IsMutex)
            {
                var existUI = FindUI<T>();
                if (existUI != null)
                {
                    return UniTask.FromResult(existUI);
                }
            }

            var tcs = new UniTaskCompletionSource<T>();
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
                    tcs.TrySetResult(null);
                    return;
                }
                
                SetupUI(ui, instance);
                ui.State = UIState.Opened;
                _openList.Add(ui);
                ui.Open(paramList);
                tcs.TrySetResult(ui);
            });
            return tcs.Task;
        }

        private void HandleOpenList()
        {
            if (_openList.Count > 0)
            {
                foreach (var ui in _openList)
                {
                    _uiList.Add(ui);
                }
                _openList.Clear();
            }
        }

        public void CloseUI(BaseUI ui)
        {
            if (ui != null && !_closeList.Contains(ui))
            {
                ui.State = UIState.Closing;
                ui.Close();
                _closeList.Add(ui);
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
            foreach (var ui in _uiList)
            {
                CloseUI(ui);
            }
        }

        private void HandleCloseList()
        {
            if (_closeList.Count > 0)
            {
                foreach (var ui in _closeList)
                {
                    CloseUIInternal(ui);
                    _uiList.Remove(ui);
                }
                _closeList.Clear();
            }
        }

        public T FindUI<T>() where T : BaseUI
        {
            return _uiList.Find((ui) => ui.GetType() == typeof(T) && (ui.State is UIState.Opening or UIState.Opened)) as T;
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
            if (_canvasTransform.TryGetValue(depthMode, out var parent))
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

            _canvasRoot = rectTrans;
        }
    }
}