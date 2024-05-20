using System;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class Player : AgentBase
    {
        
        public int CurEnergy { get; private set; }
        public int MaxEnergy { get; private set; }
        
        public int DrawCardCount { get; set; }
        
        private readonly CardDeck PoolDeck_; // 抽牌堆
        private readonly CardDeck HandDeck_; // 手牌堆
        private readonly CardDeck UsedDeck_; // 弃牌堆
        private readonly CardDeck BackDeck_; // 备用堆

        public Player()
            : base("player", 30, 30)
        {
            PoolDeck_ = new CardDeck(CardDeckType.Pool);
            HandDeck_ = new CardDeck(CardDeckType.Hand);
            UsedDeck_ = new CardDeck(CardDeckType.Used);
            BackDeck_ = new CardDeck(CardDeckType.Back);

            CurEnergy = 3;
            MaxEnergy = 5;

            DrawCardCount = BattleConst.DrawCardCountPerRound;
        }

        public override void RoundEnd()
        {
            base.RoundEnd();

            CurEnergy = MaxEnergy;
            LiteRuntime.Get<EventSystem>().Send<PlayerEnergyChangeEvent>();
        }

        public void ChangeEnergy(int value, bool changeLimit)
        {
            if (changeLimit)
            {
                MaxEnergy = Math.Clamp(MaxEnergy + value, 0, 999);
            }
            else
            {
                CurEnergy = Math.Clamp(CurEnergy + value, 0, MaxEnergy);
            }
            LiteRuntime.Get<EventSystem>().Send<PlayerEnergyChangeEvent>();
        }

        public CardDeck[] GetCardDecks()
        {
            return new[] { PoolDeck_, HandDeck_, UsedDeck_, BackDeck_ };
        }

        public CardDeck GetCardDeck(CardDeckType deckType)
        {
            switch (deckType)
            {
                case CardDeckType.Pool:
                    return PoolDeck_;
                case CardDeckType.Hand:
                    return HandDeck_;
                case CardDeckType.Used:
                    return UsedDeck_;
                case CardDeckType.Back:
                    return BackDeck_;
            }

            return null;
        }
    }
}