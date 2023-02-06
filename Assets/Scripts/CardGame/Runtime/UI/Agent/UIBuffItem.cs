using LiteCard.GamePlay;
using TMPro;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UIBuffItem : UIItemBase<BuffData>
    {
        public UIBuffItem(GameObject go, BuffData buff)
            : base(go, buff)
        {
        }
        
        public override void RefreshInfo()
        {
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "LabelName").text = Data.Cfg.Name;
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "LabelLayer").text = $"{Data.Layer}";
        }
    }
}