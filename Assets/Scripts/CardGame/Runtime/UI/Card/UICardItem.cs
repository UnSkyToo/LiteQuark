using LiteCard.GamePlay;
using TMPro;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UICardItem : UIItemBase<CardBase>
    {
        public float BaseScale { get; set; } = 1.0f;
        public bool IsSelected { get; private set; }

        public UICardItem(GameObject go, CardBase card)
            : base(go, card)
        {
            Select(false);
        }

        public void DoCast()
        {
            CardSystem.Instance.CastCard(Data, null);
        }

        public void DoCast(AgentBase target)
        {
            CardSystem.Instance.CastCard(Data, target);
        }

        public void Select(bool value)
        {
            UIUtils.SetActive(Go, "Select", value);
            IsSelected = value;
            
            OnItemChange();
        }

        public override void RefreshInfo()
        {
            var labelCost = UIUtils.FindComponent<TextMeshProUGUI>(Go, "Cost/LabelCost");
            labelCost.text = $"{Data.GetCost()}";

            var labelName = UIUtils.FindComponent<TextMeshProUGUI>(Go, "Name/LabelName");
            labelName.text = Data.GetName();
            labelName.color = Data.GetUpgradeLevel() > 0 ? Color.green : Color.white;

            var labelType = UIUtils.FindComponent<TextMeshProUGUI>(Go, "Type/LabelType");
            labelType.text = Data.GetCfg().Type.ToString();

            var labelDesc = UIUtils.FindComponent<TextMeshProUGUI>(Go, "Desc/LabelDesc");
            var desc = Data.GetCastData().Cfg.Desc;
            labelDesc.text = FormulaUtils.FormatCardDescription(AgentSystem.Instance.GetPlayer(), Data);
        }
    }
}