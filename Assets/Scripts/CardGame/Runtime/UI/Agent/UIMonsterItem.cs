using LiteCard.GamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace LiteCard.UI
{
    public sealed class UIMonsterItem : UIAgentItem
    {
        private readonly Color OriginColor_;
        
        public UIMonsterItem(GameObject go, AgentBase agent)
            : base(go, agent)
        {
            go.GetComponent<UIMonsterItemHandler>().MonsterItem = this;

            OriginColor_ = Go.GetComponent<Image>().color;
        }

        public void OnCardLocked(UICardItem item)
        {
            if (item == null)
            {
                Go.GetComponent<Image>().color = OriginColor_;
            }
            else
            {
                Go.GetComponent<Image>().color = OriginColor_ * 1.2f;
            }
        }
    }
}