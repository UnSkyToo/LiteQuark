using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class CardCastCheckHandler : BattleHandlerBase<CardCastCheckHandler>
    {
        private delegate bool Handler(Player caster, AgentBase target, CardBase card, object[] paramList);
        private readonly Dictionary<CardCastCheckType, Handler> HandlerList_;

        public CardCastCheckHandler()
        {
            HandlerList_ = new Dictionary<CardCastCheckType, Handler>
            {
                { CardCastCheckType.None, null },
                { CardCastCheckType.Forbid, Handle_Forbid },
                { CardCastCheckType.HandCardType, Handle_HandCardType },
            };
        }

        public override EditorObjectArrayResult GetObjectArrayResult(object binder)
        {
            switch (binder)
            {
                case CardCastCheckType.None:
                    return EditorObjectArrayResult.None;
                case CardCastCheckType.Forbid:
                    return EditorObjectArrayResult.None;
                case CardCastCheckType.HandCardType:
                    return new EditorObjectArrayResult("类型", "数量", typeof(CardType), typeof(int));
            }

            return EditorObjectArrayResult.None;
        }

        public bool Execute(Player caster, AgentBase target, CardBase card)
        {
            var cfg = card.GetCastData().Cfg;
            
            if (cfg.CheckType == CardCastCheckType.None)
            {
                return true;
            }

            var func = HandlerList_[cfg.CheckType];
            if (func == null)
            {
                return false;
            }

            if (!CheckParam(cfg.CheckType, cfg.CheckParams))
            {
                return false;
            }

            return func.Invoke(caster, target, card, cfg.CheckParams);
        }

        private bool Handle_Forbid(Player caster, AgentBase target, CardBase card, object[] paramList)
        {
            return false;
        }

        private bool Handle_HandCardType(Player caster, AgentBase target, CardBase card, object[] paramList)
        {
            var cardType = (CardType)paramList[0];
            var count = (int)paramList[1];

            var cardCount = GameUtils.GetCardCountByType(CardDeckType.Hand, cardType);
            if (count < 0)
            {
                return cardCount == GameUtils.GetCardCount(CardDeckType.Hand);
            }

            return GameUtils.GetCardCount(CardDeckType.Hand) >= count;
        }
    }
}