using System;
using System.Collections.Generic;
using LiteCard.UI;

namespace LiteCard.GamePlay
{
    public sealed class CardCastTargetHandler : BattleHandlerBase<CardCastTargetHandler>
    {
        private delegate void Handler(object[] paramList, Action<bool> callback);
        private readonly Dictionary<CardCastTargetType, Handler> HandlerList_;

        public CardCastTargetHandler()
        {
            HandlerList_ = new Dictionary<CardCastTargetType, Handler>
            {
                { CardCastTargetType.None, null },
                { CardCastTargetType.SelectCardByUI, Handle_SelectCardByUI },
                { CardCastTargetType.SelectCardByIndex, Handle_SelectCardByIndex },
                { CardCastTargetType.SelectCardByType, Handle_SelectCardByType },
                { CardCastTargetType.SelectCardByRandom, Handle_SelectCardByRandom },
                { CardCastTargetType.SelectCardByRandomJob, Handle_SelectCardByRandomJob },
            };
        }

        public override EditorObjectArrayResult GetObjectArrayResult(object binder)
        {
            switch (binder)
            {
                case CardCastTargetType.None:
                    return EditorObjectArrayResult.None;
                case CardCastTargetType.SelectCardByUI:
                    var selectUIResult = new EditorObjectArrayResult("牌堆", "卡牌类型", "数量", typeof(CardDeckType), typeof(CardType), typeof(int));
                    selectUIResult.SetAttrs(1, new EditorDataEnumFlagAttribute());
                    return selectUIResult;
                case CardCastTargetType.SelectCardByIndex:
                    return new EditorObjectArrayResult("牌堆", "位置", typeof(CardDeckType), typeof(int));
                case CardCastTargetType.SelectCardByType:
                    return new EditorObjectArrayResult("牌堆", "卡牌类型", "是否取反", typeof(CardDeckType), typeof(CardType), typeof(bool));
                case CardCastTargetType.SelectCardByRandom:
                    return new EditorObjectArrayResult("牌堆", "数量", typeof(CardDeckType), typeof(int));
                case CardCastTargetType.SelectCardByRandomJob:
                    var randomJobResult = new EditorObjectArrayResult("职业", "卡牌类型", "数量", typeof(CharacterJob), typeof(CardType), typeof(int));
                    randomJobResult.SetAttrs(1, new EditorDataEnumFlagAttribute());
                    return randomJobResult;
            }

            return EditorObjectArrayResult.None;
        }

        public void Execute(CardBase card, Action<bool> callback)
        {
            var cfg = card.GetCastData().Cfg;
            
            if (cfg.TargetType == CardCastTargetType.None)
            {
                callback?.Invoke(true);
                return;
            }
            
            var func = HandlerList_[cfg.TargetType];
            if (func == null)
            {
                return;
            }
            
            if (!CheckParam(cfg.TargetType, cfg.TargetParams))
            {
                callback?.Invoke(false);
                return;
            }
            
            func.Invoke(cfg.TargetParams, callback);
        }
        
        private void Handle_SelectCardByUI(object[] paramList, Action<bool> callback)
        {
            var ui = UIManager.Instance.OpenUI<UICardList>(paramList);
            ui.BindCallback((cards) =>
            {
                BattleContext.Current[BattleContextKey.SelectCardList] = new List<CardBase>(cards);
                callback?.Invoke(true);
            });
        }

        private void Handle_SelectCardByIndex(object[] paramList, Action<bool> callback)
        {
            var deck = AgentSystem.Instance.GetPlayer().GetCardDeck((CardDeckType)paramList[0]);
            var index = (int)paramList[1];

            BattleContext.Current[BattleContextKey.SelectCardList] = new List<CardBase>() { deck.Get(index) };
            callback?.Invoke(true);
        }

        private void Handle_SelectCardByType(object[] paramList, Action<bool> callback)
        {
            var deck = AgentSystem.Instance.GetPlayer().GetCardDeck((CardDeckType)paramList[0]);
            var cardType = (CardType)paramList[1];
            var inverse = (bool)paramList[2];
            var result = new List<CardBase>();

            foreach (var card in deck.GetCards())
            {
                if (inverse)
                {
                    if (card.GetCfg().Type != cardType)
                    {
                        result.Add(card);
                    }
                }
                else
                {
                    if (card.GetCfg().Type == cardType)
                    {
                        result.Add(card);
                    }
                }
            }

            BattleContext.Current[BattleContextKey.SelectCardList] = result;
            callback?.Invoke(true);
        }

        private void Handle_SelectCardByRandom(object[] paramList, Action<bool> callback)
        {
            var deck = AgentSystem.Instance.GetPlayer().GetCardDeck((CardDeckType)paramList[0]);
            var count = (int)paramList[1];
            var result = new List<CardBase>(deck.GetCards());

            if (deck.Count <= count)
            {
                BattleContext.Current[BattleContextKey.SelectCardList] = result;
            }
            else
            {
                while (result.Count > count)
                {
                    result.RemoveAt(GameUtils.RandInt(0, result.Count));
                }

                BattleContext.Current[BattleContextKey.SelectCardList] = result;
            }
            
            callback?.Invoke(true);
        }
        
        private void Handle_SelectCardByRandomJob(object[] paramList, Action<bool> callback)
        {
            var job = (CharacterJob)paramList[0];
            var cardType = (CardType)paramList[1];
            var count = (int)paramList[2];
            
            var result = new List<CardBase>();
            
            // get card from job car deck
            

            BattleContext.Current[BattleContextKey.SelectCardList] = result;
            callback?.Invoke(true);
        }
    }
}