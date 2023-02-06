using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class CardUpgradeHandler : BattleHandlerBase<CardUpgradeHandler>
    {
        private delegate void Handler(AgentBase caster, CardBase card, object[] paramList);
        private readonly Dictionary<CardUpgradeType, Handler> HandlerList_;

        public CardUpgradeHandler()
        {
            HandlerList_ = new Dictionary<CardUpgradeType, Handler>
            {
                { CardUpgradeType.None, null },
                { CardUpgradeType.AddBuff, Handle_AddBuff },
            };
        }

        public override EditorObjectArrayResult GetObjectArrayResult(object binder)
        {
            switch (binder)
            {
                case CardUpgradeType.None:
                    return EditorObjectArrayResult.None;
                case CardUpgradeType.AddBuff:
                    var buffResult = new EditorObjectArrayResult("BuffID", "层数", typeof(int), typeof(int));
                    buffResult.SetAttrs(0, new EditorDataPopupAttribute(EditorDataPopupType.Buff));
                    return buffResult;
            }
            
            return EditorObjectArrayResult.None;
        }

        public void Execute(AgentBase caster, CardBase card)
        {
            var cfg = card.GetCfg().Upgrade;

            if (cfg.Type == CardUpgradeType.None)
            {
                return;
            }
            
            var func = HandlerList_[cfg.Type];
            if (func == null)
            {
                return;
            }

            if (!CheckParam(cfg.Type, cfg.Params))
            {
                return;
            }

            func.Invoke(caster, card, cfg.Params);
        }

        private void Handle_AddBuff(AgentBase caster, CardBase card, object[] paramList)
        {
            var buffID = (int)paramList[0];
            var layer = (int)paramList[1];
            
            BuffSystem.Instance.AddBuff(caster, buffID, layer);
        }
    }
}