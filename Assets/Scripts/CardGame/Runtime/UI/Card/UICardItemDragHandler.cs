using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LiteCard.UI
{
    public sealed class UICardItemDragHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public UICardItem CardItem { get; set; }

        private Canvas Canvas_;
        private RectTransform RectTrans_;
        private RectTransform RootUIRectTrans_;
        
        private UICardArrowItem ArrowItem_;
        private UIMonsterItemHandler CurrentHoveredHandler_;

        private Vector2 CardPosition_;
        private Vector2 DragOrigin_;

        private int Order_ = 0;

        private void Awake()
        {
            Canvas_ = GetComponent<Canvas>();
            RectTrans_ = GetComponent<RectTransform>();
            Order_ = Canvas_.sortingOrder;

            RootUIRectTrans_ = LiteRuntime.Get<UISystem>().FindUI<UICardHand>().Go.GetComponent<RectTransform>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.667f * CardItem.BaseScale;
            RectTrans_.anchoredPosition = new Vector2(RectTrans_.anchoredPosition.x, 70);
            Canvas_.sortingOrder = Order_ + 1;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * CardItem.BaseScale;
            RectTrans_.anchoredPosition = new Vector2(RectTrans_.anchoredPosition.x, 0);
            Canvas_.sortingOrder = Order_;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var cfg = CardItem.Data.GetCfg();

            if (cfg.NeedTarget)
            {
                 CreateArrow(RootUIRectTrans_);
            }
            else
            {
                CardPosition_ = RectTrans_.anchoredPosition;
                DragOrigin_ = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var cfg = CardItem.Data.GetCfg();

            if (cfg.NeedTarget)
            {
                LiteRuntime.Get<AssetSystem>().UnloadGameObject(ArrowItem_.gameObject);
                ArrowItem_ = null;

                if (CurrentHoveredHandler_ != null)
                {
                    var target = CurrentHoveredHandler_.GetAgent();
                    CurrentHoveredHandler_.OnCardHovered(null);
                    CurrentHoveredHandler_ = null;

                    RectTrans_.anchoredPosition = CardPosition_;
                    CardItem.DoCast(target);
                }
            }
            else
            {
                var curPos = RectTrans_.anchoredPosition;
                var offset = curPos - CardPosition_;

                if (offset.y >= GameConst.UI.CardDragLimitY)
                {
                    RectTrans_.anchoredPosition = CardPosition_;
                    CardItem.DoCast();
                }
                else
                {
                    RectTrans_.anchoredPosition = CardPosition_;
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            var cfg = CardItem.Data.GetCfg();

            if (cfg.NeedTarget)
            {
                RefreshArrow(eventData);
                RefreshHovered(eventData);
            }
            else
            {
                var newPos = CardPosition_ + eventData.position - DragOrigin_;
                RectTrans_.anchoredPosition = newPos;
            }
        }

        private void CreateArrow(Transform parent)
        {
            LiteRuntime.Get<AssetSystem>().LoadGameObject(GameConst.Prefab.ArrowItem, (go) =>
            {
                UnityUtils.SetParent(parent, go);
                go.GetComponent<Canvas>().sortingOrder = (int)UIDepthMode.Top + 1000;
                ArrowItem_ = go.GetComponent<UICardArrowItem>();
            });
        }

        private void RefreshArrow(PointerEventData eventData)
        {
            if (ArrowItem_ != null)
            {
                var beginPos = UIUtils.WorldPosToCanvasPos(RootUIRectTrans_, transform.position);
                var endPos = UIUtils.ScreenPosToCanvasPos(RootUIRectTrans_, eventData.position, null);
                ArrowItem_.UpdatePosition(beginPos, endPos);
            }
        }

        private void RefreshHovered(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                var monsterItemHandler = UIUtils.FindComponentUpper<UIMonsterItemHandler>(eventData.pointerCurrentRaycast.gameObject);
                if (monsterItemHandler != null && CurrentHoveredHandler_ != monsterItemHandler)
                {
                    CurrentHoveredHandler_?.OnCardHovered(null);
                    monsterItemHandler.OnCardHovered(CardItem);
                    CurrentHoveredHandler_ = monsterItemHandler;
                }
                else if (monsterItemHandler == null && CurrentHoveredHandler_ != null)
                {
                    CurrentHoveredHandler_.OnCardHovered(null);
                    CurrentHoveredHandler_ = null;
                }
            }
            else
            {
                if (CurrentHoveredHandler_ != null)
                {
                    CurrentHoveredHandler_.OnCardHovered(null);
                    CurrentHoveredHandler_ = null;
                }
            }
        }
    }
}