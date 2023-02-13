using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class CardConfig : IJsonMainConfig
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        [EditorDataReadOnly]
        public int ID { get; private set; }
        public string Name { get; private set; }
        [EditorDataAsset(typeof(UnityEngine.Sprite))]
        public string IconRes { get; private set; }
        public CharacterJob Job { get; private set; }
        public CardRarity Rarity { get; private set; }
        public CardType Type { get; private set; }
        public int Number { get; private set; }
        public bool NeedTarget { get; private set; }
        public CardCastConfig CastNormal { get; private set; }
        public CardCastConfig CastUpgrade { get; private set; }
        public CardUpgradeConfig Upgrade { get; private set; }
        public BuffSet[] InitBuffs { get; private set; }
        public CardTag[] Tags { get; private set; }

        public CardConfig()
        {
        }

        public int GetMainID()
        {
            return ID;
        }

        public object Clone()
        {
            var result = new CardConfig
            {
                ID = ID,
                Name = Name,
                IconRes = IconRes,
                Job = Job,
                Rarity = Rarity,
                Type = Type,
                Number = Number,
                NeedTarget = NeedTarget,
                CastNormal = CastNormal.Clone() as CardCastConfig,
                CastUpgrade = CastUpgrade.Clone() as CardCastConfig,
                Upgrade = Upgrade.Clone() as CardUpgradeConfig,
                InitBuffs = TypeUtils.CloneDataArray(InitBuffs),
                Tags = TypeUtils.CloneObjectArray(Tags)
            };
            return result;
        }
    }
}