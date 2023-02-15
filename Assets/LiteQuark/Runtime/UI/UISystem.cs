using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public sealed class UISystem : ISystem, ITick
    {
        private const int UIStepOrder = 100;
        
        private readonly Dictionary<UIDepthMode, Transform> CanvasTransform_ = new Dictionary<UIDepthMode, Transform>();
        private readonly List<UIBase> UIList_ = new List<UIBase>();
        private readonly List<UIBase> CloseList_ = new List<UIBase>();

        public UISystem()
        {
            var parent = GameObject.Find("Canvas").transform;
            foreach (var depthModeName in Enum.GetNames(typeof(UIDepthMode)))
            {
                var depthMode = Enum.Parse<UIDepthMode>(depthModeName);
                var go = UnityUtils.CreateGameObject(parent, $"Canvas_{depthModeName}");
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

        public T OpenUI<T>(params object[] paramList) where T : UIBase
        {
            var ui = Activator.CreateInstance<T>();

            LiteRuntime.Get<AssetSystem>().InstantiateAsync(ui.PrefabPath, (instance) =>
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

        public void CloseUI(UIBase ui)
        {
            CloseList_.Add(ui);
        }

        private void CloseUIInternal(UIBase ui)
        {
            UIBinder.AutoUnbind(ui);
            ui.Close();
            LiteRuntime.Get<AssetSystem>().UnloadAsset(ui.Go);
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

        public T FindUI<T>() where T : UIBase
        {
            return UIList_.Find((ui) => ui.GetType() == typeof(T)) as T;
        }

        private void SetupUI(UIBase ui, GameObject instance)
        {
            var parent = GetUIParent(ui.DepthMode);
            UnityUtils.SetParent(parent, instance);
            
            var rectTransform = UnityUtils.GetOrCreateComponent<RectTransform>(instance);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var canvas = UnityUtils.GetOrCreateComponent<Canvas>(instance);
            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)ui.DepthMode + (parent.childCount - 1) * UIStepOrder;

            var raycaster = UnityUtils.GetOrCreateComponent<GraphicRaycaster>(instance);
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
    }
}