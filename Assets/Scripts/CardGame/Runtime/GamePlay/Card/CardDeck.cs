using System;
using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class CardDeck
    {
        public CardDeckType DeckType { get; }
        public int Count => CardList_.Count;
        
        private readonly List<CardBase> CardList_ = new List<CardBase>();
        
        public CardDeck(CardDeckType deckType)
        {
            DeckType = deckType;
        }

        public bool Empty()
        {
            return Count == 0;
        }
        
        public void Clear()
        {
            CardList_.Clear();
        }

        public void Add(CardBase card)
        {
            card.BelongDeckType = DeckType;
            CardList_.Add(card);
        }

        public void Add(CardBase[] cards)
        {
            foreach (var card in cards)
            {
                Add(card);
            }
        }

        public void Add(CardBase card, int index)
        {
            card.BelongDeckType = DeckType;
            index = Math.Clamp(index, 0, CardList_.Count);
            CardList_.Insert(index, card);
        }

        public CardBase Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                return null;
            }

            return CardList_[index];
        }

        public CardBase GetRandom()
        {
            var index = MathUtils.RandInt(0, Count);
            return Get(index);
        }

        public int GetCount(CardTag tag)
        {
            var count = 0;
            
            foreach (var card in CardList_)
            {
                if (card.ContainTag(tag))
                {
                    count++;
                }
            }

            return count;
        }

        public int GetCount(CardType type)
        {
            var count = 0;

            foreach (var card in CardList_)
            {
                if (card.GetCfg().Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        public void Push(CardBase card)
        {
            Add(card, 0);
        }

        public CardBase Pop()
        {
            var card = Get(0);
            Remove(card);
            return card;
        }

        public void Remove(int index)
        {
            var card = Get(index);
            Remove(card);
        }

        public void Remove(CardBase card)
        {
            if (card == null)
            {
                return;
            }

            card.BelongDeckType = CardDeckType.None;
            CardList_.Remove(card);
        }

        public void Shuffle()
        {
            var rand = new Random((int)DateTime.Now.Ticks);
            var result = new List<CardBase>();

            foreach (var card in CardList_)
            {
                result.Insert(rand.Next(0, result.Count), card);
            }
            
            Clear();
            CardList_.AddRange(result);
        }

        public CardBase[] GetCards()
        {
            return CardList_.ToArray();
        }

        public CardBase[] GetCards(int cardID)
        {
            var result = new List<CardBase>();

            foreach (var card in CardList_)
            {
                if (card.GetCfg().ID == cardID)
                {
                    result.Add(card);
                }
            }

            return result.ToArray();
        }
    }
}