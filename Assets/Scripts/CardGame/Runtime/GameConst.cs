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
        }

        public static class Preload
        {
            public static int Count => PrefabList.Length + JsonList.Length;
            
            public static readonly string[] PrefabList =
            {
                Prefab.ArrowItem,
                Prefab.CardItem,
                Prefab.PlayerItem,
                Prefab.MonsterItem,
                Prefab.BuffItem,
                
                "CardGame/Prefab/UI/Card/UICardHand.prefab",
                "CardGame/Prefab/UI/Card/UICardList.prefab",
                "CardGame/Prefab/UI/Agent/UIAgent.prefab",
                "CardGame/Prefab/UI/Battle/UIBattleMain.prefab",
            };

            public static readonly string[] JsonList =
            {
                "CardGame/Json/card.json",
                "CardGame/Json/buff.json",
                "CardGame/Json/modifier.json",
                "CardGame/Json/match.json",
            };
        }

        public static class UI
        {
            public const int CardDragLimitY = 300;
        }
    }
}