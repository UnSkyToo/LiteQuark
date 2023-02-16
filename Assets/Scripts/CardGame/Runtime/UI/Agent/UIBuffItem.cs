using LiteCard.GamePlay;
using LiteQuark.Runtime;
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
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "LabelLayer").text = $"{Data.Layer}";
            UIUtils.ReplaceSprite(Go, Data.Cfg.IconRes, false);
        }
    }
}