using LiteQuark.Runtime;
using TMPro;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UILoading : UIBase
    {
        public override string PrefabPath => "CardGame/Prefab/UI/UILoading.prefab";
        public override UIDepthMode DepthMode => UIDepthMode.Normal;

        private int TotalCount_;
        private int CurrentCount_;

        public UILoading()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            SetPercent(0);

            TotalCount_ = GameConst.Preload.Count;
            CurrentCount_ = 0;

            void OnLoadCallback(bool isLoaded, string path)
            {
                if (!isLoaded)
                {
                    Log.Error($"preload asset error : {path}");
                    return;
                }
                
                CurrentCount_++;
                SetPercent((float)CurrentCount_ / (float)TotalCount_);
                SetTips($"{CurrentCount_}/{TotalCount_}");

                if (CurrentCount_ >= TotalCount_)
                {
                    LiteRuntime.Get<UISystem>().CloseUI(this);
                }
            }
            
            foreach (var prefabPath in GameConst.Preload.PrefabList)
            {
                LiteRuntime.Get<AssetSystem>().PreloadAsset<GameObject>(prefabPath, (isLoad) =>
                {
                    OnLoadCallback(isLoad, prefabPath);
                });
            }

            foreach (var jsonPath in GameConst.Preload.JsonList)
            {
                LiteRuntime.Get<AssetSystem>().PreloadAsset<TextAsset>(jsonPath, (isLoad) =>
                {
                    OnLoadCallback(isLoad, jsonPath);
                });
            }
        }

        protected override void OnClose()
        {
        }

        private void SetPercent(float val)
        {
            val = Mathf.Clamp01(val);
            var rect = FindComponent<RectTransform>("ImageBarBG/ImageBar");
            rect.sizeDelta = new Vector2(1876f * val, 26f);
        }

        private void SetTips(string text)
        {
            FindComponent<TextMeshProUGUI>("LabelTips").text = text;
        }
    }
}