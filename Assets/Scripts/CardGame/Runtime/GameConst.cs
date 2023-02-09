namespace LiteCard
{
    public static class GameConst
    {
        public static class Prefab
        {
            public const string ArrowItem = "CardGame/Prefab/UI/Card/ArrowItem.prefab";
            public const string CardItem = "CardGame/Prefab/UI/Card/CardItem.prefab";

            public const string PlayerItem = "CardGame/Prefab/UI/Agent/PlayerItem.prefab";
            public const string MonsterItem = "CardGame/Prefab/UI/Agent/MonsterItem.prefab";
            public const string BuffItem = "CardGame/Prefab/UI/Agent/BuffItem.prefab";

            public static readonly string[] PreloadList =
            {
                ArrowItem,
                CardItem,
                PlayerItem,
                MonsterItem,
                BuffItem,
                
                "CardGame/Prefab/UI/Card/UICardHand.prefab",
                "CardGame/Prefab/UI/Card/UICardList.prefab",
                "CardGame/Prefab/UI/Agent/UIAgent.prefab",
                "CardGame/Prefab/UI/Battle/UIBattleMain.prefab",
            };
        }

        public static class UI
        {
            public const int CardDragLimitY = 300;
        }
    }
}