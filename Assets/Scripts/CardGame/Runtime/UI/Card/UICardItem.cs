﻿using LiteCard.GamePlay;
using LiteQuark.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LiteCard.UI
{
    public sealed class UICardItem : UIItemBase<CardBase>
    {
        public float BaseScale { get; set; } = 0.6f;
        public bool IsSelected { get; private set; }

        public UICardItem(GameObject go, CardBase card)
            : base(go, card)
        {
            go.transform.localScale = Vector3.one * BaseScale;
            Setup();
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

        private void Setup()
        {
            UIUtils.ReplaceSprite(Go, "Background", GameConst.Card.BackgroundResPathList[Data.GetCfg().Job][Data.GetCfg().Type], false);
            UIUtils.ReplaceSprite(Go, "Icon", Data.GetCfg().IconRes, false);
            UIUtils.ReplaceSprite(Go, "Name", GameConst.Card.NameResPathList[Data.GetCfg().Rarity], false);
            UIUtils.ReplaceSprite(Go, "Type", GameConst.Card.TypeResPathList[Data.GetCfg().Type][Data.GetCfg().Rarity], false);
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
            labelDesc.text = FormulaUtils.FormatCardDescription(AgentSystem.Instance.GetPlayer(), Data);
        }
    }
}