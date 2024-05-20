using LiteCard.GamePlay;
using LiteQuark.Runtime;
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
            
            LiteRuntime.Get<EventSystem>().Register<AgentAttrChangeEvent>(OnAgentAttrChangeEvent);
            LiteRuntime.Get<EventSystem>().Register<BuffLayerChangeEvent>(OnBuffLayerChangeEvent);
        }

        public override void Dispose()
        {
            LiteRuntime.Get<EventSystem>().UnRegister<AgentAttrChangeEvent>(OnAgentAttrChangeEvent);
            LiteRuntime.Get<EventSystem>().UnRegister<BuffLayerChangeEvent>(OnBuffLayerChangeEvent);

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
            
            RefreshBuffList();
        }

        private void RefreshBuffList()
        {
            BuffList_.RefreshInfo(Data.GetBuffList(), (buff) => buff.Cfg.ShowIcon);
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
            
            RefreshBuffList();
        }
    }
}