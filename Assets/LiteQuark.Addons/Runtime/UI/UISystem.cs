using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public sealed class UISystem : ISystem, ISystemSettingProvider<UISystemSetting>, ITick, ILateTick
    {
        public UISystemSetting Setting { get; set; }

        private const int UIStepOrder = 100;

        public RectTransform CanvasRoot => _canvasRoot;
        public Camera UICamera { get; private set; }

        private RectTransform _canvasRoot;
        private readonly Dictionary<UIDepthMode, Transform> _canvasTransform = new();
        private readonly List<BaseUI> _openList = new();
        private readonly List<BaseUI> _uiList = new();
        private readonly List<BaseUI> _closeList = new();
        private readonly Dictionary<Type, AsyncLazy<BaseUI>> _mutexOpens = new();
        private readonly HashSet<GameObjectHandle> _pendingHandles = new();
        private bool _isClosing;

        public UISystem()
        {
        }

        public UniTask<bool> Initialize(CancellationToken ct = default)
        {
            var parent = new GameObject("Canvas", typeof(RectTransform));
            _canvasRoot = (RectTransform)parent.transform;
            UnityEngine.Object.DontDestroyOnLoad(parent);
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

            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            _isClosing = true;
            // Cancellation resumes OpenUI synchronously; detach before its finally removes the handle.
            var pendingHandles = new List<GameObjectHandle>(_pendingHandles);
            _pendingHandles.Clear();
            foreach (var handle in pendingHandles)
            {
                handle.Dispose();
            }

            HandleOpenList();
            CloseAllUI();
            HandleCloseList();

            if (_canvasRoot != null)
            {
                UnityEngine.Object.Destroy(_canvasRoot.gameObject);
                _canvasRoot = null;
            }
            _canvasTransform.Clear();
            UICamera = null;

            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            HandleOpenList();
            HandleCloseList();

            foreach (var ui in _uiList)
            {
                if (ui.State == UIState.Opened)
                {
                    ui.Update(deltaTime);
                }
            }
        }

        public void LateTick(float deltaTime)
        {
            // OpenUI can finish after Update. Include those windows without advancing either queue.
            var pendingCount = _openList.Count;
            LateTickOpened(_uiList, _uiList.Count, deltaTime);
            LateTickOpened(_openList, pendingCount, deltaTime);
        }

        private static void LateTickOpened(List<BaseUI> windows, int count, float deltaTime)
        {
            var runtime = LiteRuntime.Instance;
            for (var i = 0; i < count && i < windows.Count && runtime.CanLateTick; i++)
            {
                var ui = windows[i];
                if (ui.State == UIState.Opened && ui is ILateTick lateTick)
                {
                    lateTick.LateTick(deltaTime);
                }
            }
        }

        public UniTask<T> OpenUI<T>(UIConfig config, params object[] paramList) where T : BaseUI
        {
            return OpenUI<T>(config, CancellationToken.None, paramList);
        }

        public UniTask<T> OpenUI<T>(UIConfig config, CancellationToken ct, params object[] paramList) where T : BaseUI
        {
            if (_isClosing)
            {
                return UniTask.FromException<T>(new ObjectDisposedException(nameof(UISystem)));
            }

            if (ct.IsCancellationRequested)
            {
                return UniTask.FromCanceled<T>(ct);
            }

            return config.IsMutex ? OpenMutexUI<T>(config, ct, paramList) : OpenNewUI<T>(config, ct, paramList);
        }

        private async UniTask<T> OpenMutexUI<T>(UIConfig config, CancellationToken ct, object[] paramList) where T : BaseUI
        {
            var type = typeof(T);
            if (_mutexOpens.TryGetValue(type, out var opening))
            {
                return (T)await opening.Task.AttachExternalCancellation(ct);
            }

            var existing = FindUI<T>();
            if (existing != null)
            {
                return existing;
            }

            opening = new AsyncLazy<BaseUI>(OpenAndRemove);
            _mutexOpens.Add(type, opening);
            return (T)await opening.Task;

            async UniTask<BaseUI> OpenAndRemove()
            {
                try
                {
                    return await OpenNewUI<T>(config, ct, paramList);
                }
                finally
                {
                    _mutexOpens.Remove(type);
                }
            }
        }

        private async UniTask<T> OpenNewUI<T>(UIConfig config, CancellationToken ct, object[] paramList) where T : BaseUI
        {
            var ui = Activator.CreateInstance<T>();
            ui.Config = config;
            ui.System = this;
            ui.State = UIState.Opening;
            var parent = GetUIParent(config.DepthMode);
            var handle = LiteRuntime.Asset.InstantiateHandle(config.PrefabPath, parent, ct: ct);
            _pendingHandles.Add(handle);

            try
            {
                await handle;
                var instance = handle.Result;
                if (instance == null)
                {
                    ui.State = UIState.Error;
                    ReleaseUI(ui);
                    handle.Dispose();
                    LLog.Error("Load ui prefab error : {0}", config.PrefabPath);
                    return null;
                }

                SetupUI(ui, instance, handle);
                // Binding can synchronously invoke callbacks that release the caller.
                ct.ThrowIfCancellationRequested();
                ui.State = UIState.Opened;
                var openCount = _openList.Count;
                ui.Open(paramList);
                if (_openList.Count != openCount)
                {
                    // OnOpen can synchronously publish a cached UI that took this sorting order.
                    ui.SetSortingOrder(RefreshSortingOrders(config.DepthMode));
                }
                _openList.Add(ui);
                return ui;
            }
            catch (OperationCanceledException) when (_isClosing || ct.IsCancellationRequested)
            {
                ui.State = UIState.Closed;
                ReleaseUI(ui);
                handle.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                ui.State = UIState.Error;
                ReleaseUI(ui);
                handle.Dispose();
                LLog.Error("Load ui prefab error : {0}, {1}", config.PrefabPath, ex.Message);
                return null;
            }
            finally
            {
                _pendingHandles.Remove(handle);
            }
        }

        private void ReleaseUI(BaseUI ui)
        {
            if (ui.Go != null)
            {
                UIBinder.AutoUnbind(ui);
            }
            ui.ReleaseResources();
            ui.ReleaseGo();
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
            if (ui != null && ui.State == UIState.Opened)
            {
                ui.State = UIState.Closing;
                try
                {
                    ui.Close();
                }
                finally
                {
                    _closeList.Add(ui);
                }
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
            ReleaseUI(ui);
        }

        public void CloseAllUI()
        {
            var pendingCount = _openList.Count;
            foreach (var ui in _uiList)
            {
                CloseUI(ui);
            }
            for (var i = 0; i < pendingCount; i++)
            {
                CloseUI(_openList[i]);
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
                    RefreshSortingOrders(ui.Config.DepthMode);
                }
                _closeList.Clear();
            }
        }

        public T FindUI<T>() where T : BaseUI
        {
            static bool IsOpenType(BaseUI ui) => ui.State == UIState.Opened && ui.GetType() == typeof(T);

            return (_uiList.Find(IsOpenType) ?? _openList.Find(IsOpenType)) as T;
        }

        private void SetupUI(BaseUI ui, GameObject instance, GameObjectHandle handle)
        {
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
            canvas.sortingOrder = RefreshSortingOrders(ui.Config.DepthMode);

            var raycaster = instance.GetOrAddComponent<GraphicRaycaster>();
            raycaster.blockingMask = LayerMask.GetMask("UI");

            ui.BindGo(instance, handle);
            UIBinder.AutoBind(ui);
        }

        private int RefreshSortingOrders(UIDepthMode depthMode)
        {
            var order = (int)depthMode;
            if (depthMode == UIDepthMode.Scene)
            {
                return order;
            }

            order = RefreshSortingOrders(_uiList, depthMode, order);
            return RefreshSortingOrders(_openList, depthMode, order);
        }

        private static int RefreshSortingOrders(List<BaseUI> windows, UIDepthMode depthMode, int order)
        {
            foreach (var ui in windows)
            {
                if (ui.Config.DepthMode == depthMode)
                {
                    ui.SetSortingOrder(order);
                    order += UIStepOrder;
                }
            }

            return order;
        }

        private Transform GetUIParent(UIDepthMode depthMode)
        {
            if (_canvasTransform.TryGetValue(depthMode, out var parent))
            {
                return parent;
            }

            LLog.Error("Can't find canvas : {0}", depthMode);
            return null;
        }

        private void SetupCanvasRoot(GameObject go)
        {
            go.layer = LayerMask.NameToLayer("UI");

            var configuredCamera = Setting.UICamera;
            UICamera = configuredCamera != null ? configuredCamera : Camera.main;
            var canvas = go.GetOrAddComponent<Canvas>();
            canvas.renderMode = UICamera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = UICamera;

            var scaler = go.GetOrAddComponent<CanvasScaler>();
            scaler.uiScaleMode = Setting.ScaleMode;
            scaler.screenMatchMode = Setting.MatchMode;
            scaler.referenceResolution = new Vector2(Setting.ResolutionWidth, Setting.ResolutionHeight);
            scaler.matchWidthOrHeight = Setting.MatchValue;
            scaler.referencePixelsPerUnit = 100;

            if (configuredCamera != null)
            {
                configuredCamera.orthographic = true;
                configuredCamera.orthographicSize = Screen.height / (2f * scaler.referencePixelsPerUnit);
            }

            var raycaster = go.GetOrAddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
        }
    }
}
