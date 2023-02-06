using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class UISystem : ISystem, ITick
    {
        private readonly List<UIBase> UIList_ = new List<UIBase>();
        private readonly List<UIBase> CloseList_ = new List<UIBase>();

        public UISystem()
        {
        }

        public void Dispose()
        {
            CloseAllUI();
            HandleCloseList();
            Tick(0);
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

            LiteRuntime.Get<AssetSystem>().LoadGameObject(ui.PrefabPath, (instance) =>
            {
                if (instance == null)
                {
                    LLog.Error($"ui prefab load error : {ui.PrefabPath}");
                    return;
                }

                var canvas = GameObject.Find("Canvas");
                instance.transform.SetParent(canvas.transform, false);
                ui.BindGo(instance);
                UIBinder.AutoBind(ui);
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
            LiteRuntime.Get<AssetSystem>().UnloadGameObject(ui.Go);
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
    }
}