using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UIManager : Singleton<UIManager>
    {
        private readonly List<UIBase> UIList_ = new List<UIBase>();
        private readonly List<UIBase> CloseList_ = new List<UIBase>();

        public UIManager()
        {
        }

        public void Cleanup()
        {
            CloseAllUI();
            HandleCloseList();
            Update(0);
        }

        public T OpenUI<T>(params object[] paramList) where T : UIBase
        {
            var ui = Activator.CreateInstance<T>();

            LiteRuntime.Get<AssetSystem>().LoadGameObject(ui.PrefabPath, (instance) =>
            {
                if (instance == null)
                {
                    Log.Error($"ui prefab load error : {ui.PrefabPath}");
                    return;
                }

                var canvas = GameObject.Find("Canvas");
                instance.transform.SetParent(canvas.transform, false);
                ui.BindGo(instance);
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

        public void Update(float deltaTime)
        {
            HandleCloseList();
            
            foreach (var ui in UIList_)
            {
                ui.Update(deltaTime);
            }
        }
    }
}