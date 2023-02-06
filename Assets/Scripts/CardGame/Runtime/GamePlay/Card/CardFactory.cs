namespace LiteCard.GamePlay
{
    public static class CardFactory
    {
        public static CardBase CreateCard(int cardID)
        {
            try
            {
                var cardData = new CardData(cardID);
                switch (cardData.Cfg.Type)
                {
                    case CardType.Attack:
                        return new AttackCard(cardData);
                    case CardType.Skill:
                        return new SkillCard(cardData);
                    case CardType.State:
                        return new StateCard(cardData);
                }
                
                Log.Error($"error card type : {cardData.Cfg.Type} - {cardID}");
                return null;
            }
            catch
            {
                Log.Error($"error card id : {cardID}");
                return null;
            }
        }
    }
}