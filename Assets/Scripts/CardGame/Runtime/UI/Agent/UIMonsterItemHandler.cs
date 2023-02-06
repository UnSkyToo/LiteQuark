using LiteCard.GamePlay;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UIMonsterItemHandler : MonoBehaviour
    {
        public UIMonsterItem MonsterItem { get; set; }
        
        public void OnCardHovered(UICardItem item)
        {
            MonsterItem.OnCardLocked(item);
        }

        public AgentBase GetAgent()
        {
            return MonsterItem.Data;
        }
    }
}