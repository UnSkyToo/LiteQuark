namespace LiteCard.GamePlay
{
    public sealed class CardUpgradeConfig : IJsonConfig
    {
#if UNITY_EDITOR
        public bool IsFoldout = true;
#endif
        public int Limit { get; private set; }
        public CardUpgradeType Type { get; private set; }
        [EditorObjectArray(EditorObjectArrayType.CardUpgrade, nameof(Type))]
        public object[] Params { get; private set; }
        
        public CardUpgradeConfig()
        {
        }

        public object Clone()
        {
            var result = new CardUpgradeConfig
            {
                Limit = Limit,
                Type = Type,
                Params = TypeUtils.CloneObjectArray(Params)
            };
            return result;
        }
    }
}