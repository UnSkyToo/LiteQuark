using System;
using System.Collections.Generic;
using LiteCard.GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LiteCard.UI
{
    public sealed class UICardList : UIBase
    {
        public override string PrefabPath => "CardGame/UI/Card/UICardList.prefab";
        
        private UIItemList<UICardItem, CardBase> CardList_;
        private CardDeckType DeckType_;
        private CardType CardType_;
        private int MaxCount_;
        private Action<CardBase[]> Callback_;

        public UICardList()
        {
        }
        
        protected override void OnOpen(params object[] paramList)
        {
            UIUtils.FindComponent<Button>(Go, "BtnConfirm").onClick.AddListener(OnBtnConfirmClick);
            CardList_ = new UIItemList<UICardItem, CardBase>(
                UIUtils.FindChild(Go, "Content"),
                GameConst.Prefab.CardItem);
            CardList_.ItemCreate += OnItemCreate;
            CardList_.ItemChange += OnItemChange;

            DeckType_ = (CardDeckType)paramList[0];
            CardType_ = (CardType)paramList[1];
            MaxCount_ = (int)paramList[2];
            
            UIUtils.SetActive(Go, "LabelCount", MaxCount_ >= 0);

            RefreshInfo();
        }

        protected override void OnClose()
        {
            CardList_.ItemChange -= OnItemChange;
            CardList_.ItemCreate -= OnItemCreate;
            CardList_.Dispose();
            UIUtils.FindComponent<Button>(Go, "BtnConfirm").onClick.RemoveListener(OnBtnConfirmClick);
        }

        private void OnItemCreate(int index, UICardItem item)
        {
            if (MaxCount_ >= 0)
            {
                item.Go.AddComponent<UICardItemSelectHandler>().CardItem = item;
            }

            item.Go.GetComponent<Canvas>().sortingOrder = 6;
        }

        private void OnItemChange(UICardItem item)
        {
            UIUtils.FindComponent<TextMeshProUGUI>(Go, "LabelCount").text = $"{GetSelectList().Count}/{MaxCount_}";
        }

        public void BindCallback(Action<CardBase[]> callback)
        {
            Callback_ = callback;
        }

        public void RefreshInfo()
        {
            CardList_?.RefreshInfo(GetCards());

            if (MaxCount_ >= 0)
            {
                UIUtils.FindComponent<TextMeshProUGUI>(Go, "LabelCount").text = $"{GetSelectList().Count}/{MaxCount_}";
            }
        }

        private CardBase[] GetCards()
        {
            var deck = AgentSystem.Instance.GetPlayer().GetCardDeck(DeckType_);
            
            var result = new List<CardBase>();
            foreach (var card in deck.GetCards())
            {
                if (CardType_ == CardType.None || (card.GetCfg().Type & CardType_) == card.GetCfg().Type)
                {
                    result.Add(card);
                }
            }

            return result.ToArray();
        }

        private List<CardBase> GetSelectList()
        {
            var result = new List<CardBase>();
            
            foreach (var item in CardList_.GetItemList())
            {
                if (item.IsSelected)
                {
                    result.Add(item.Data);
                }
            }
            
            return result;
        }

        private void OnBtnConfirmClick()
        {
            if (MaxCount_ >= 0)
            {
                var result = GetSelectList();

                if (result.Count == 0 && CardList_.GetItemList().Length > 0)
                {
                    Log.Warning("please select card");
                    return;
                }

                if (result.Count > MaxCount_)
                {
                    Log.Warning($"only select {MaxCount_} card");
                    return;
                }

                Callback_?.Invoke(result.ToArray());
            }

            UIManager.Instance.CloseUI(this);
        }
    }
}