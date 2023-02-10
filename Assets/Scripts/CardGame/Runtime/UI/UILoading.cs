using LiteQuark.Runtime;
using TMPro;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UILoading : UIBase
    {
        public override string PrefabPath => "CardGame/Prefab/UI/UILoading.prefab";
        public override UIDepthMode DepthMode => UIDepthMode.Normal;

        public UILoading()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            SetPercent(0);

            Preloader.Instance.Progress += OnProgress;
            Preloader.Instance.Load();
        }

        protected override void OnClose()
        {
            Preloader.Instance.Progress -= OnProgress;
        }

        private void OnProgress(int cur, int max)
        {
            SetPercent((float)cur / (float)max);
            SetTips($"{cur}/{max}");

            if (cur >= max)
            {
                LiteRuntime.Get<UISystem>().CloseUI(this);
            }
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