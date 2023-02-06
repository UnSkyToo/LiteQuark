using UnityEngine;
using UnityEngine.EventSystems;

namespace LiteCard.UI
{
    public sealed class UICardItemSelectHandler : MonoBehaviour, IPointerClickHandler
    {
        public UICardItem CardItem { get; set; }

        private void Awake()
        {
            GetComponent<Canvas>().sortingOrder = 1;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            CardItem.Select(!CardItem.IsSelected);
        }
    }
}