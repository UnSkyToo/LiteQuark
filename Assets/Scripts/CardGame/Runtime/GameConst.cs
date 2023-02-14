using System.Collections.Generic;
using LiteCard.GamePlay;

namespace LiteCard
{
    public static class GameConst
    {
        public static class Prefab
        {
            public const string ArrowItem = "CardGame/Prefab/UI/Battle/CardArrowItem.prefab";
            public const string CardItem = "CardGame/Prefab/UI/Card/CardItem.prefab";

            public const string PlayerItem = "CardGame/Prefab/UI/Agent/PlayerItem.prefab";
            public const string MonsterItem = "CardGame/Prefab/UI/Agent/MonsterItem.prefab";
            public const string BuffItem = "CardGame/Prefab/UI/Agent/BuffItem.prefab";
        }

        public static class Card
        {
            public static readonly Dictionary<CharacterJob, Dictionary<CardType, string>> BackgroundResPathList = new Dictionary<CharacterJob, Dictionary<CardType, string>>
            {
                {
                    CharacterJob.Normal, new Dictionary<CardType, string>
                    {
                        {CardType.Attack, "CardGame/Image/cardui/cardui3/512_bg_attack_gray.png"},
                        {CardType.Skill, "CardGame/Image/cardui/cardui4/512_bg_skill_gray.png"},
                        {CardType.Power, "CardGame/Image/cardui/cardui2/512_bg_power_gray.png"},
                        {CardType.State, "CardGame/Image/cardui/cardui4/512_bg_skill_gray.png"},
                        {CardType.Curse, "CardGame/Image/cardui/cardui4/512_bg_skill_gray.png"},
                    }
                },
                {
                    CharacterJob.Ironclad, new Dictionary<CardType, string>
                    {
                        {CardType.Attack, "CardGame/Image/cardui/cardui4/512_bg_attack_red.png"},
                        {CardType.Skill, "CardGame/Image/cardui/cardui4/512_bg_skill_red.png"},
                        {CardType.Power, "CardGame/Image/cardui/cardui4/512_bg_power_red.png"},
                        {CardType.State, "CardGame/Image/cardui/cardui4/512_bg_skill_red.png"},
                        {CardType.Curse, "CardGame/Image/cardui/cardui4/512_bg_skill_red.png"},
                    }
                },
                {
                    CharacterJob.Silent, new Dictionary<CardType, string>
                    {
                        {CardType.Attack, "CardGame/Image/cardui/cardui1/512_bg_attack_green.png"},
                        {CardType.Skill, "CardGame/Image/cardui/cardui4/512_bg_skill_green.png"},
                        {CardType.Power, "CardGame/Image/cardui/cardui4/512_bg_power_green.png"},
                        {CardType.State, "CardGame/Image/cardui/cardui4/512_bg_skill_green.png"},
                        {CardType.Curse, "CardGame/Image/cardui/cardui4/512_bg_skill_green.png"},
                    }
                },
                {
                    CharacterJob.Defect, new Dictionary<CardType, string>
                    {
                        {CardType.Attack, "CardGame/Image/cardui/cardui1/512_bg_attack_blue.png"},
                        {CardType.Skill, "CardGame/Image/cardui/cardui4/512_bg_skill_blue.png"},
                        {CardType.Power, "CardGame/Image/cardui/cardui2/512_bg_power_blue.png"},
                        {CardType.State, "CardGame/Image/cardui/cardui4/512_bg_skill_blue.png"},
                        {CardType.Curse, "CardGame/Image/cardui/cardui4/512_bg_skill_blue.png"},
                    }
                },
                {
                    CharacterJob.Watcher, new Dictionary<CardType, string>
                    {
                        {CardType.Attack, "CardGame/Image/cardui/cardui4/512_bg_attack_purple.png"},
                        {CardType.Skill, "CardGame/Image/cardui/cardui4/512_bg_skill_purple.png"},
                        {CardType.Power, "CardGame/Image/cardui/cardui4/512_bg_power_purple.png"},
                        {CardType.State, "CardGame/Image/cardui/cardui4/512_bg_skill_purple.png"},
                        {CardType.Curse, "CardGame/Image/cardui/cardui4/512_bg_skill_purple.png"},
                    }
                },
            };

            public static readonly Dictionary<CardRarity, string> NameResPathList = new Dictionary<CardRarity, string>
            {
                { CardRarity.Starter, "CardGame/Image/cardui/cardui1/512_banner_common.png" },
                { CardRarity.Common, "CardGame/Image/cardui/cardui1/512_banner_common.png" },
                { CardRarity.Uncommon, "CardGame/Image/cardui/cardui2/512_banner_uncommon.png" },
                { CardRarity.Rare, "CardGame/Image/cardui/cardui1/512_banner_rare.png" },
            };

            public static readonly Dictionary<CardType, Dictionary<CardRarity, string>> TypeResPathList = new Dictionary<CardType, Dictionary<CardRarity, string>>
            {
                {
                    CardType.Attack, new Dictionary<CardRarity, string>
                    {
                        { CardRarity.Starter, "CardGame/Image/cardui/cardui2/512_frame_attack_common.png" },
                        { CardRarity.Common, "CardGame/Image/cardui/cardui2/512_frame_attack_common.png" },
                        { CardRarity.Uncommon, "CardGame/Image/cardui/cardui1/512_frame_attack_uncommon.png" },
                        { CardRarity.Rare, "CardGame/Image/cardui/cardui2/512_frame_attack_rare.png" },
                    }
                },
                {
                    CardType.Skill, new Dictionary<CardRarity, string>
                    {
                        { CardRarity.Starter, "CardGame/Image/cardui/cardui1/512_frame_skill_common.png" },
                        { CardRarity.Common, "CardGame/Image/cardui/cardui1/512_frame_skill_common.png" },
                        { CardRarity.Uncommon, "CardGame/Image/cardui/cardui2/512_frame_skill_uncommon.png" },
                        { CardRarity.Rare, "CardGame/Image/cardui/cardui2/512_frame_skill_rare.png" },
                    }
                },
                {
                    CardType.Power, new Dictionary<CardRarity, string>
                    {
                        { CardRarity.Starter, "CardGame/Image/cardui/cardui1/512_frame_power_common.png" },
                        { CardRarity.Common, "CardGame/Image/cardui/cardui1/512_frame_power_common.png" },
                        { CardRarity.Uncommon, "CardGame/Image/cardui/cardui1/512_frame_power_uncommon.png" },
                        { CardRarity.Rare, "CardGame/Image/cardui/cardui1/512_frame_power_rare.png" },
                    }
                },
                {
                    CardType.State, new Dictionary<CardRarity, string>
                    {
                        { CardRarity.Starter, "CardGame/Image/cardui/cardui1/512_frame_skill_common.png" },
                        { CardRarity.Common, "CardGame/Image/cardui/cardui1/512_frame_skill_common.png" },
                        { CardRarity.Uncommon, "CardGame/Image/cardui/cardui2/512_frame_skill_uncommon.png" },
                        { CardRarity.Rare, "CardGame/Image/cardui/cardui2/512_frame_skill_rare.png" },
                    }
                },
                {
                    CardType.Curse, new Dictionary<CardRarity, string>
                    {
                        { CardRarity.Starter, "CardGame/Image/cardui/cardui1/512_frame_skill_common.png" },
                        { CardRarity.Common, "CardGame/Image/cardui/cardui1/512_frame_skill_common.png" },
                        { CardRarity.Uncommon, "CardGame/Image/cardui/cardui2/512_frame_skill_uncommon.png" },
                        { CardRarity.Rare, "CardGame/Image/cardui/cardui2/512_frame_skill_rare.png" },
                    }
                },
            };
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