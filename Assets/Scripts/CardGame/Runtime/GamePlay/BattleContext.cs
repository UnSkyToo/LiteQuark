using System.Collections.Generic;

namespace LiteCard.GamePlay
{
    public enum BattleContextKey
    {
        SelectTarget,
        EventTargetList,

        Damage,
        Armour,
        
        DeductArmour,
        DeductHp,
        
        ChangeAttrType,
        ChangeAttrValue,
        
        CastCost,
        
        DrawCard,
        CastCard,
        
        SelectCardList,
        ConsumeCardList,
        ConsumeCardCount,
    }
    
    public sealed class BattleContext
    {
        public static BattleContext Current { get; } = new BattleContext();
        
        private readonly Dictionary<BattleContextKey, object> Cache_ = new Dictionary<BattleContextKey, object>();

        public object this[BattleContextKey key]
        {
            get
            {
                if (!Cache_.TryGetValue(key, out var val))
                {
                    return null;
                }

                return val;
            }
            set
            {
                if (!Cache_.ContainsKey(key))
                {
                    Cache_.Add(key, value);
                }
                else
                {
                    Cache_[key] = value;
                }
            }
        }

        protected BattleContext()
        {
        }

        public void Reset()
        {
            Cache_.Clear();
        }

        public List<CardBase> GetCardList(BattleContextKey key)
        {
            var result = new List<CardBase>();
            
            if (this[key] is CardBase card)
            {
                result.Add(card);
            }
            else if (this[key] is List<CardBase> cardList)
            {
                result.AddRange(cardList);
            }
            else if (this[key] is CardBase[] cardArray)
            {
                result.AddRange(cardArray);
            }

            return result;
        }
    }
}