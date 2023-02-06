using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class ModifierHandler : BattleHandlerBase<ModifierHandler>
    {
        private delegate void Handler(AgentBase caster, AgentBase target, ModifierData modifier);
        private readonly Dictionary<ModifierType, Handler> HandlerList_;
        
        public ModifierHandler()
        {
            HandlerList_ = new Dictionary<ModifierType, Handler>
            {
                { ModifierType.Damage, Handle_DamageModifier },
                { ModifierType.DamageThorns, Handle_DamageThornsModifier },
                { ModifierType.DamageFixed, Handle_DamageFixedModifier },

                { ModifierType.Recovery, Handle_RecoveryModifier },

                { ModifierType.Armour, Handle_ArmourModifier },
                { ModifierType.ArmourFixed, Handle_ArmourFixedModifier },

                { ModifierType.Buff, Handle_BuffModifier },

                { ModifierType.CopyCard, Handle_CopyCardModifier },
                { ModifierType.MoveCard, Handle_MoveCardModifier },
                { ModifierType.DrawCard, Handle_DrawCardModifier },
                { ModifierType.AddCard, Handle_AddCardModifier },
                { ModifierType.CastCard, Handle_CastCardModifier },
                { ModifierType.ConsumeCard, Handle_ConsumeCardModifier },
                { ModifierType.UpgradeCard, Handle_UpgradeCardModifier },

                { ModifierType.CardCost, Handle_CardCostModifier },
                { ModifierType.CardCostByKey, Handle_CardCostByKeyModifier },
                { ModifierType.CardTag, Handle_CardTagModifier },

                { ModifierType.Attr, Handle_AttrModifier },
                { ModifierType.ChangeEnergy, Handle_ChangeEnergyModifier },
                { ModifierType.AgentTag, Handle_AgentTagModifier },
            };
        }

        public override EditorObjectArrayResult GetObjectArrayResult(object binder)
        {
            switch (binder)
            {
                case ModifierType.Damage:
                    return new EditorObjectArrayResult("公式", typeof(string));
                case ModifierType.DamageThorns:
                    return new EditorObjectArrayResult("基础值", "比例", typeof(int), typeof(float));
                case ModifierType.DamageFixed:
                    return new EditorObjectArrayResult("比例", typeof(float));
                case ModifierType.Recovery:
                    return new EditorObjectArrayResult("公式", typeof(string));
                case ModifierType.Armour:
                    return new EditorObjectArrayResult("公式", typeof(string));
                case ModifierType.ArmourFixed:
                    return new EditorObjectArrayResult("比例", typeof(float));
                case ModifierType.Buff:
                    var buffResult = new EditorObjectArrayResult("BuffID", "层数", typeof(int), typeof(int));
                    buffResult.SetAttrs(0, new EditorDataPopupAttribute(EditorDataPopupType.Buff));
                    return buffResult;
                case ModifierType.CopyCard:
                    return new EditorObjectArrayResult("ContextKey", "牌堆", "数量", typeof(BattleContextKey), typeof(CardDeckType), typeof(int));
                case ModifierType.MoveCard:
                    return new EditorObjectArrayResult("ContextKey", "牌堆", "位置", typeof(BattleContextKey), typeof(CardDeckType), typeof(int));
                case ModifierType.DrawCard:
                    return new EditorObjectArrayResult("数量", typeof(int));
                case ModifierType.AddCard:
                    var addCardResult = new EditorObjectArrayResult("牌堆", "卡牌ID", "数量", typeof(CardDeckType), typeof(int), typeof(int));
                    addCardResult.SetAttrs(1, new EditorDataPopupAttribute(EditorDataPopupType.Card));
                    return addCardResult;
                case ModifierType.CastCard:
                    var castCardResult = new EditorObjectArrayResult("ContextKey", "MatchID", typeof(BattleContextKey), typeof(int));
                    castCardResult.SetAttrs(1, new EditorDataPopupAttribute(EditorDataPopupType.Match));
                    return castCardResult;
                case ModifierType.ConsumeCard:
                    return new EditorObjectArrayResult("ContextKey", typeof(BattleContextKey));
                case ModifierType.UpgradeCard:
                    return new EditorObjectArrayResult("ContextKey", "作用域", typeof(BattleContextKey), typeof(RecordScopeType));
                case ModifierType.CardCost:
                    var cardCostResult = new EditorObjectArrayResult("牌组", "卡牌ID", "作用域", "费用", typeof(CardDeckType), typeof(int), typeof(RecordScopeType), typeof(int));
                    cardCostResult.SetAttrs(1, new EditorDataPopupAttribute(EditorDataPopupType.Card));
                    return cardCostResult;
                case ModifierType.CardCostByKey:
                    return new EditorObjectArrayResult("ContextKey", "作用域", "费用", typeof(BattleContextKey), typeof(RecordScopeType), typeof(int));
                case ModifierType.CardTag:
                    return new EditorObjectArrayResult("ContextKey", "作用域", "Tag", typeof(BattleContextKey), typeof(RecordScopeType), typeof(CardTag));
                case ModifierType.Attr:
                    return new EditorObjectArrayResult("属性类型", "值", "百分比", typeof(AgentAttrType), typeof(int), typeof(float));
                case ModifierType.ChangeEnergy:
                    return new EditorObjectArrayResult("值", "改变上限", typeof(int), typeof(bool));
                case ModifierType.AgentTag:
                    return new EditorObjectArrayResult("作用域", "Tag", typeof(RecordScopeType), typeof(AgentTag));
            }
            
            return EditorObjectArrayResult.None;
        }

        public void Execute(AgentBase caster, List<AgentBase> targets, ModifierData modifier)
        {
            var func = HandlerList_[modifier.Cfg.Type];
            if (func == null)
            {
                return;
            }

            if (!CheckParam(modifier.Cfg.Type, modifier.ParamList))
            {
                return;
            }

            foreach (var target in targets)
            {
                var repeatCount = FormulaUtils.Calculate(caster, target, modifier.Cfg.RepeatCount);

                for (var index = 0; index < repeatCount; ++index)
                {
                    func.Invoke(caster, target, modifier);
                }
            }
        }
        
        private void Handle_DamageModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var formula = (string)modifier.ParamList[0];
            var value = FormulaUtils.Calculate(caster, target, formula);
            DamageSystem.Instance.Handle(caster, target, value);
        }

        private void Handle_DamageThornsModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var baseValue = (int)modifier.ParamList[0];
            var percent = (float)modifier.ParamList[1];

            var damageData = BattleContext.Current[BattleContextKey.Damage] as DamageData;

            var value = GameUtils.CalculateFloorValue(baseValue + damageData.Value * percent);
            DamageSystem.Instance.Handle(caster, target, value);
        }

        private void Handle_DamageFixedModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var percent = (float)modifier.ParamList[0];
            var damageData = BattleContext.Current[BattleContextKey.Damage] as DamageData;

            damageData.Value = GameUtils.CalculateFloorValue(damageData.Value, percent);
        }

        private void Handle_RecoveryModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var formula = (string)modifier.ParamList[0];
            var value = FormulaUtils.Calculate(caster, target, formula);
            target.ChangeAttr(AgentAttrType.CurHp, value, 0);
        }

        private void Handle_ArmourModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var formula = (string)modifier.ParamList[0];
            var value = FormulaUtils.Calculate(caster, target, formula);
            ArmourSystem.Instance.Handle(caster, target, value);
        }

        private void Handle_ArmourFixedModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var percent = (float)modifier.ParamList[0];
            var armourData = BattleContext.Current[BattleContextKey.Armour] as ArmourData;

            armourData.Value = GameUtils.CalculateFloorValue(armourData.Value, percent);
        }
        
        private void Handle_BuffModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var buffID = (int)modifier.ParamList[0];
            var layer = (int)modifier.ParamList[1];

            BuffSystem.Instance.BuffLayerChange(caster, target, buffID, layer);
        }

        private void Handle_CopyCardModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var contextKey = (BattleContextKey)modifier.ParamList[0];
            var deckType = (CardDeckType)modifier.ParamList[1];
            var count = (int)modifier.ParamList[2];

            var targetDeck = AgentSystem.Instance.GetPlayer().GetCardDeck(deckType);
            foreach (var card in BattleContext.Current.GetCardList(contextKey))
            {
                for (var index = 0; index < count; ++index)
                {
                    targetDeck.Add(card.Clone());
                }
            }
        }

        private void Handle_MoveCardModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var contextKey = (BattleContextKey)modifier.ParamList[0];
            var targetDeckType = (CardDeckType)modifier.ParamList[1];
            var targetIndex = (int)modifier.ParamList[2];

            foreach (var card in BattleContext.Current.GetCardList(contextKey))
            {
                CardSystem.Instance.MoveCard(card, targetDeckType, targetIndex);
            }
        }

        private void Handle_DrawCardModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var count = (int)modifier.ParamList[0];
            CardSystem.Instance.DrawCard(count);
        }

        private void Handle_AddCardModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var deckType = (CardDeckType)modifier.ParamList[0];
            var cardID = (int)modifier.ParamList[1];
            var count = (int)modifier.ParamList[2];

            for (var index = 0; index < count; ++index)
            {
                CardSystem.Instance.AddCardWithID(deckType, cardID);
            }
        }
        
        private void Handle_CastCardModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var contextKey = (BattleContextKey)modifier.ParamList[0];
            var matchID = (int)modifier.ParamList[1];

            var newTargets = AgentMatcher.Match(caster, new List<AgentBase> { target }, matchID);

            foreach (var newTarget in newTargets)
            {
                foreach (var card in BattleContext.Current.GetCardList(contextKey))
                {
                    CardSystem.Instance.CastCard(card, newTarget);
                }
            }
        }

        private void Handle_ConsumeCardModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var contextKey = (BattleContextKey)modifier.ParamList[0];
            CardSystem.Instance.ConsumeCard(BattleContext.Current.GetCardList(contextKey).ToArray());
        }
        
        private void Handle_UpgradeCardModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var contextKey = (BattleContextKey)modifier.ParamList[0];
            var scopeType = (RecordScopeType)modifier.ParamList[1];
            
            foreach (var card in BattleContext.Current.GetCardList(contextKey))
            {
                CardSystem.Instance.UpgradeCard(caster, card, scopeType);
            }
        }

        private void Handle_CardCostModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var deckType = (CardDeckType)modifier.ParamList[0];
            var cardID = (int)modifier.ParamList[1];
            var scopeType = (RecordScopeType)modifier.ParamList[2];
            var decCost = (int)modifier.ParamList[3];

            var deck = AgentSystem.Instance.GetPlayer().GetCardDeck(deckType);
            var cards = deck.GetCards(cardID);

            foreach (var card in cards)
            {
                card.ChangeDecCost(scopeType, decCost == -999 ? -card.GetCost() : decCost);
            }
        }
        
        private void Handle_CardCostByKeyModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var contextKey = (BattleContextKey)modifier.ParamList[0];
            var scopeType = (RecordScopeType)modifier.ParamList[1];
            var decCost = (int)modifier.ParamList[2];
            
            foreach (var card in BattleContext.Current.GetCardList(contextKey))
            {
                card.ChangeDecCost(scopeType, decCost == -999 ? -card.GetCost() : decCost);
            }
        }
        
        private void Handle_CardTagModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var contextKey = (BattleContextKey)modifier.ParamList[0];
            var scopeType = (RecordScopeType)modifier.ParamList[1];
            var tag = (CardTag)modifier.ParamList[2];

            foreach (var card in BattleContext.Current.GetCardList(contextKey))
            {
                card.AddTag(scopeType, tag);
            }
        }

        private void Handle_AttrModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var attrType = (AgentAttrType)modifier.ParamList[0];
            var changeValue = (int)modifier.ParamList[1];
            var changePercent = (float)modifier.ParamList[2];

            target.ChangeAttr(attrType, changeValue, changePercent);
        }

        private void Handle_ChangeEnergyModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var value = (int)modifier.ParamList[0];
            var changeLimit = (bool)modifier.ParamList[1];

            AgentSystem.Instance.GetPlayer().ChangeEnergy(value, changeLimit);
        }

        private void Handle_AgentTagModifier(AgentBase caster, AgentBase target, ModifierData modifier)
        {
            var scopeType = (RecordScopeType)modifier.ParamList[0];
            var tag = (AgentTag)modifier.ParamList[1];

            target.AddTag(scopeType, tag);
        }
    }
}