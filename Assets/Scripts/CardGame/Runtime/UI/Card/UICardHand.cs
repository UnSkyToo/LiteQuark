using LiteCard.GamePlay;
using LiteQuark.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LiteCard.UI
{
    public sealed class UICardHand : UIBase
    {
        public override string PrefabPath => "CardGame/UI/Card/UICardHand.prefab";

        private UIItemList<UICardItem, CardBase> CardList_;
        private CardDeck Deck_;

        public UICardHand()
        {
        }

        protected override void OnOpen(params object[] paramList)
        {
            Deck_ = (CardDeck)paramList[0];
            
            CardList_ = new UIItemList<UICardItem, CardBase>(
                UIUtils.FindChild(Go, "Bottom/Content"),
                GameConst.Prefab.CardItem);
            CardList_.ItemCreate += OnItemCreate;
            
            UIUtils.FindComponent<Button>(Go, "BtnPool").onClick.AddListener(OnBtnPoolClick);
            UIUtils.FindComponent<Button>(Go, "BtnUsed").onClick.AddListener(OnBtnUsedClick);
            UIUtils.FindComponent<Button>(Go, "BtnAddCard").onClick.AddListener(OnBtnAddCardClick);
            
            LiteRuntime.Get<EventSystem>().Register<CardChangeEvent>(OnCardChangeEvent);
            
            RefreshInfo();
        }

        protected override void OnClose()
        {
            CardList_.ItemCreate -= OnItemCreate;
            CardList_.Dispose();
            
            UIUtils.FindComponent<Button>(Go, "BtnPool").onClick.RemoveListener(OnBtnPoolClick);
            UIUtils.FindComponent<Button>(Go, "BtnUsed").onClick.RemoveListener(OnBtnUsedClick);
            UIUtils.FindComponent<Button>(Go, "BtnAddCard").onClick.RemoveListener(OnBtnAddCardClick);
            
            LiteRuntime.Get<EventSystem>().UnRegister<CardChangeEvent>(OnCardChangeEvent);
        }
        
        public CardDeck GetDeck()
        {
            return Deck_;
        }

        public void RefreshInfo()
        {
            CardList_?.RefreshInfo(Deck_.GetCards());

            UIUtils.FindComponent<TextMeshProUGUI>(Go, "BtnPool/LabelNum").text = GameUtils.GetCardCount(CardDeckType.Pool).ToString();
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "BtnUsed/LabelNum").text = GameUtils.GetCardCount(CardDeckType.Used).ToString();
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
            
            item.Go.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);
            item.Go.GetComponent<RectTransform>().localScale = new Vector3(scale, scale, scale);

            item.Go.AddComponent<UICardItemDragHandler>().CardItem = item;
        }

        private void OnCardChangeEvent(CardChangeEvent evt)
        {
            RefreshInfo();
        }

        private void OnBtnPoolClick()
        {
            UIManager.Instance.OpenUI<UICardList>(CardDeckType.Pool, -1);
        }

        private void OnBtnUsedClick()
        {
            UIManager.Instance.OpenUI<UICardList>(CardDeckType.Used, -1);
        }

        private void OnBtnAddCardClick()
        {
            var text = UIUtils.FindComponent<TMP_InputField>(Go, "InputCardID").text;
            if (int.TryParse(text, out var cardID))
            {
                CardSystem.Instance.AddCardWithID(CardDeckType.Hand, cardID);
                RefreshInfo();
            }
        }
    }
}