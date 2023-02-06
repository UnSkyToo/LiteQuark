using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public sealed class CardSystem : Singleton<CardSystem>, IRoundLifeCycle
    {
        private readonly Player Player_;

        public CardSystem()
        {
            Player_ = AgentSystem.Instance.GetPlayer();
        }

        public void RoundBegin()
        {
            DrawCard(Player_.DrawCardCount);
        }
        
        public void RoundEnd()
        {
            var handDeck = Player_.GetCardDeck(CardDeckType.Hand);
            foreach (var card in handDeck.GetCards())
            {
                // 虚无
                if (card.ContainTag(CardTag.Ethereal))
                {
                    ConsumeCard(card);
                }
                else
                {
                    AddCard(CardDeckType.Used, card, false);
                }
            }
            handDeck.Clear();
        }

        public void AddCardWithID(CardDeckType deckType, int cardID)
        {
            var card = CardFactory.CreateCard(cardID);
            AddCard(deckType, card, true);
        }
        
        public void AddCard(CardDeckType deckType, CardBase card, bool isNew)
        {
            if (card == null)
            {
                return;
            }

            var deck = Player_.GetCardDeck(deckType);
            deck.Add(card);

            if (isNew)
            {
                var initBuffs = card.GetCfg().InitBuffs;
                foreach (var buff in card.GetCfg().InitBuffs)
                {
                    BuffSystem.Instance.AddBuff(Player_, buff.BuffID, buff.Layer);
                }
            }
        }
        
        public void RemoveCard(CardBase card)
        {
            var deck = Player_.GetCardDeck(card.BelongDeckType);
            deck?.Remove(card);
        }
        
        public void DrawCard(int count)
        {
            if (Player_.ContainTag(AgentTag.ForbidDrawCard))
            {
                Log.Info("forbid draw card");
                return;
            }
            
            for (var index = 0; index < count; ++index)
            {
                DrawCard();
            }
        }

        private void DrawCard()
        {
            var poolDeck = Player_.GetCardDeck(CardDeckType.Pool);
            if (poolDeck.Empty())
            {
                FlushPoolCard();
            }

            var card = poolDeck.Pop();
            if (card == null)
            {
                Log.Info("pool deck empty");
                return;
            }
            
            AddCard(CardDeckType.Hand, card, false);
            Log.Info($"draw card : {card.GetName()}");

            BattleContext.Current[BattleContextKey.DrawCard] = card;
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.DrawCard, Player_);
            
            EventManager.Instance.Send<CardChangeEvent>();
        }

        private void FlushPoolCard()
        {
            var poolDeck = Player_.GetCardDeck(CardDeckType.Pool);
            if (!poolDeck.Empty())
            {
                Log.Info("pool deck not empty");
                return;
            }
            
            Log.Info("flush pool card");
            var usedDeck = Player_.GetCardDeck(CardDeckType.Used);
            poolDeck.Add(usedDeck.GetCards());
            usedDeck.Clear();
            poolDeck.Shuffle();
            
            EventManager.Instance.Send<CardChangeEvent>();
        }
        
        public void ConsumeCard(CardBase card)
        {
            if (card == null)
            {
                return;
            }

            ConsumeCard(new[] { card });
        }
        
        public void ConsumeCard(CardBase[] cards)
        {
            if (cards == null)
            {
                return;
            }

            foreach (var card in cards)
            {
                RemoveCard(card);
                AddCard(CardDeckType.Back, card, false);

            }
            
            BattleContext.Current[BattleContextKey.ConsumeCardList] = new List<CardBase>(cards);
            BattleContext.Current[BattleContextKey.ConsumeCardCount] = cards.Length;
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.ConsumeCard, Player_);

            EventManager.Instance.Send<CardChangeEvent>();
        }

        public void CastCard(CardBase card, AgentBase target)
        {
            if (target == null)
            {
                if (!card.GetCfg().NeedTarget)
                {
                    target = Player_;
                }
                else
                {
                    Log.Error($"please select card target : {card.GetName()}");
                    return;
                }
            }
            
            BattleContext.Current.Reset();
            
            RemoveCard(card);
            
            CardCastTargetHandler.Instance.Execute(card, (isCast) =>
            {
                if (!isCast || !DoCastCard(Player_, target, card))
                {   
                    AddCard(CardDeckType.Hand, card, false);
                }
                else
                {
                    EventManager.Instance.Send<CardChangeEvent>();
                }
            });
        }

        private bool DoCastCard(Player caster, AgentBase target, CardBase card)
        {
            if (card == null)
            {
                return false;
            }
            
            if (target == null)
            {
                Log.Info("invalid target");
                return false;
            }

            if (!CastCardCheck(caster, target, card))
            {
                Log.Info("not meeting conditions");
                return false;
            }

            var cost = card.GetCost();
            
            Log.Info($"{caster.Name} cast <{card.GetName()}> to {target.Name}");
            BattleContext.Current[BattleContextKey.CastCard] = card;
            BattleContext.Current[BattleContextKey.CastCost] = cost;
            BattleContext.Current[BattleContextKey.SelectTarget] = target;

            if (card.Cast(caster, target))
            {
                if (!card.ContainTag(CardTag.Consume))
                {
                    AddCard(CardDeckType.Used, card, false);
                }
                else
                {
                    ConsumeCard(card);
                }
                
                caster.ChangeEnergy(-cost, false);

                BuffSystem.Instance.TriggerBuff(BuffTriggerType.AfterCastCard, caster, card.GetCfg().Type);

                return true;
            }

            return false;
        }

        private bool CastCardCheck(Player caster, AgentBase target, CardBase card)
        {
            if (!CardCastCheckHandler.Instance.Execute(caster, target, card))
            {
                return false;
            }
            
            if (card.GetCost() > caster.CurEnergy)
            {
                return false;
            }

            if (card.ContainTag(CardTag.Unplayable))
            {
                return false;
            }
            
            return true;
        }

        public bool UpgradeCard(AgentBase caster, CardBase card, RecordScopeType scopeType)
        {
            if (card.GetUpgradeLevel() >= card.GetCfg().Upgrade.Limit)
            {
                Log.Info("card upgrade count limit");
                return false;
            }

            card.Upgrade(scopeType);

            CardUpgradeHandler.Instance.Execute(caster, card);
            
            EventManager.Instance.Send<CardChangeEvent>();

            return true;
        }

        public void MoveCard(CardBase card, CardDeckType targetDeckType, int targetIndex)
        {
            var targetDeck = Player_.GetCardDeck(targetDeckType);
            if (targetIndex < 0) // random
            {
                targetIndex = GameUtils.RandInt(0, targetDeck.Count);
            }

            RemoveCard(card);
            targetDeck.Add(card, targetIndex);
        }
    }
}