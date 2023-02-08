using UnityEngine;
using UnityEngine.EventSystems;

namespace LiteCard.UI
{
    public sealed class UICardItemSelectHandler : MonoBehaviour, IPointerClickHandler
    {
        public UICardItem CardItem { get; set; }

        private void Awake()
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            CardItem.Select(!CardItem.IsSelected);
        }
    }
}