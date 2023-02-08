using LiteCard.GamePlay;
using LiteQuark.Runtime;
using TMPro;
using UnityEngine;

namespace LiteCard.UI
{
    public sealed class UICardHand : UIBase
    {
        public override string PrefabPath => "CardGame/UI/Card/UICardHand.prefab";
        public override UIDepthMode DepthMode => UIDepthMode.Normal;

        private UIItemList<UICardItem, CardBase> CardList_;
        private CardDeck Deck_;

        public UICardHand()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            Deck_ = (CardDeck)paramList[0];
            
            CardList_ = new UIItemList<UICardItem, CardBase>(
                FindChild("Bottom/Content"),
                GameConst.Prefab.CardItem);
            CardList_.ItemCreate += OnItemCreate;

            LiteRuntime.Get<EventSystem>().Register<CardChangeEvent>(OnCardChangeEvent);
            
            RefreshInfo();
        }

        protected override void OnClose()
        {
            CardList_.ItemCreate -= OnItemCreate;
            CardList_.Dispose();
            
            LiteRuntime.Get<EventSystem>().UnRegister<CardChangeEvent>(OnCardChangeEvent);
        }
        
        public CardDeck GetDeck()
        {
            return Deck_;
        }

        public void RefreshInfo()
        {
            CardList_?.RefreshInfo(Deck_.GetCards());

            FindComponent<TextMeshProUGUI>("BtnPool/LabelNum").text = GameUtils.GetCardCount(CardDeckType.Pool).ToString();
            FindComponent<TextMeshProUGUI>("BtnUsed/LabelNum").text = GameUtils.GetCardCount(CardDeckType.Used).ToString();
        }

        private void OnItemCreate(int index, UICardItem item)
        {
            var scale = 1f;
            var totalWidth = Deck_.Count * 200f;
            if (totalWidth > 1600f)
            {
                scale = 1600f / totalWidth;
            }
            
            var startX = -(Deck_.Count - 1) / 2f * 200f * scale;
            var x = startX + index * 200 * scale;
            
            UIUtils.AddSortingCanvas(item.Go, SortingOrder + 1);
            item.Go.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);
            item.Go.GetComponent<RectTransform>().localScale = new Vector3(scale, scale, scale);

            item.Go.AddComponent<UICardItemDragHandler>().CardItem = item;
        }

        private void OnCardChangeEvent(CardChangeEvent evt)
        {
            RefreshInfo();
        }

        [UIClickEvent("BtnPool")]
        private void OnBtnPoolClick()
        {
            LiteRuntime.Get<UISystem>().OpenUI<UICardList>(CardDeckType.Pool, -1);
        }
        
        [UIClickEvent("BtnUsed")]
        private void OnBtnUsedClick()
        {
            LiteRuntime.Get<UISystem>().OpenUI<UICardList>(CardDeckType.Used, -1);
        }

        [UIClickEvent("BtnAddCard")]
        private void OnBtnAddCardClick()
        {
            var text = FindComponent<TMP_InputField>("InputCardID").text;
            if (int.TryParse(text, out var cardID))
            {
                CardSystem.Instance.AddCardWithID(CardDeckType.Hand, cardID);
                RefreshInfo();
            }
        }
    }
}