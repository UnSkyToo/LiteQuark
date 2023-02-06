using LiteCard.GamePlay;
using TMPro;
using UnityEngine;

namespace LiteCard.UI
{
    public abstract class UIAgentItem : UIItemBase<AgentBase>
    {
        protected readonly UIItemList<UIBuffItem, BuffData> BuffList_;

        protected UIAgentItem(GameObject go, AgentBase agent)
            : base(go, agent)
        {
            BuffList_ = new UIItemList<UIBuffItem, BuffData>(
                UIUtils.FindChild(Go, "Buff"),
                GameConst.Prefab.BuffItem);
            
            EventManager.Instance.Register<AgentAttrChangeEvent>(OnAgentAttrChangeEvent);
            EventManager.Instance.Register<BuffLayerChangeEvent>(OnBuffLayerChangeEvent);
        }

        public override void Dispose()
        {
            EventManager.Instance.UnRegister<AgentAttrChangeEvent>(OnAgentAttrChangeEvent);
            EventManager.Instance.UnRegister<BuffLayerChangeEvent>(OnBuffLayerChangeEvent);

            BuffList_.Dispose();
            
            base.Dispose();
        }

        public override void RefreshInfo()
        {
            var curHp = Data.CurHp;
            var maxHp = Data.MaxHp;

            UIUtils.FindComponent<TextMeshProUGUI>(Go, "HP/LabelValue").text = $"{curHp}/{maxHp}";
            UIUtils.FindComponent<RectTransform>(Go, "HP/ImageBar").sizeDelta = new Vector2(186f * (float)(curHp) / (float)(maxHp), 16f);

            if (Data.Armour > 0)
            {
                UIUtils.FindComponent<TextMeshProUGUI>(Go, "Armour/LabelValue").text = $"{Data.Armour}";
                UIUtils.SetActive(Go, "Armour", true);
            }
            else
            {
                UIUtils.SetActive(Go, "Armour", false);
            }

            UIUtils.FindComponent<TextMeshProUGUI>(Go, "LabelName").text = Data.Name;
            
            BuffList_.RefreshInfo(Data.GetBuffList());
        }

        private void OnAgentAttrChangeEvent(AgentAttrChangeEvent evt)
        {
            if (evt.Agent.UniqueID != Data.UniqueID)
            {
                return;
            }
            
            RefreshInfo();
        }

        private void OnBuffLayerChangeEvent(BuffLayerChangeEvent evt)
        {
            if (evt.Agent.UniqueID != Data.UniqueID)
            {
                return;
            }
            
            BuffList_.RefreshInfo(Data.GetBuffList());
        }
    }
}