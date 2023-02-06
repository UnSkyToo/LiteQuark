using LiteQuark.Runtime;
using TMPro;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UILoading : UIBase
    {
        public override string PrefabPath => "CardGame/UI/UILoading.prefab";

        private int TotalCount_;
        private int CurrentCount_;

        public UILoading()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            SetPercent(0);

            TotalCount_ = GameConst.Prefab.PreloadList.Length;
            CurrentCount_ = 0;
            foreach (var prefabPath in GameConst.Prefab.PreloadList)
            {
                LiteRuntime.Get<AssetSystem>().PreloadAsset<GameObject>(prefabPath, (isLoad) =>
                {
                    if (!isLoad)
                    {
                        Log.Error($"preload asset error : {prefabPath}");
                        return;
                    }
                    CurrentCount_++;
                    SetPercent((float)CurrentCount_ / (float)TotalCount_);
                    SetTips($"{CurrentCount_}/{TotalCount_}");

                    if (CurrentCount_ >= TotalCount_)
                    {
                        UIManager.Instance.CloseUI(this);
                        // GameLogic.Instance.Startup();
                    }
                });
            }
        }

        protected override void OnClose()
        {
        }

        private void SetPercent(float val)
        {
            val = Mathf.Clamp01(val);
            var rect = UIUtils.FindComponent<RectTransform>(Go, "ImageBarBG/ImageBar");
            rect.sizeDelta = new Vector2(1876f * val, 26f);
        }

        private void SetTips(string text)
        {
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "LabelTips").text = text;
        }
    }
}