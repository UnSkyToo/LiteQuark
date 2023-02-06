namespace LiteCard
{
    public static class GameConst
    {
        public static class Prefab
        {
            public const string ArrowItem = "CardGame/UI/Card/ArrowItem.prefab";
            public const string CardItem = "CardGame/UI/Card/CardItem.prefab";

            public const string PlayerItem = "CardGame/UI/Agent/PlayerItem.prefab";
            public const string MonsterItem = "CardGame/UI/Agent/MonsterItem.prefab";
            public const string BuffItem = "CardGame/UI/Agent/BuffItem.prefab";

            public static readonly string[] PreloadList =
            {
                ArrowItem,
                CardItem,
                PlayerItem,
                MonsterItem,
                BuffItem,
                
                "CardGame/UI/Card/UICardHand.prefab",
                "CardGame/UI/Card/UICardList.prefab",
                "CardGame/UI/Agent/UIAgent.prefab",
                "CardGame/UI/Battle/UIBattleMain.prefab",
            };
        }

        public static class UI
        {
            public const int CardDragLimitY = 300;
        }
    }
}